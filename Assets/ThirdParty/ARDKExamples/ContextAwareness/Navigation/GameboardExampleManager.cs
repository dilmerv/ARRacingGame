// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections;
using System.Collections.Generic;

using Niantic.ARDK.Extensions.Meshing;
using Niantic.ARDKExamples.Gameboard;

using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARDKExamples
{
    public class GameboardExampleManager: MonoBehaviour
    {
#pragma warning disable 0649
        [SerializeField]
        private Camera _arCamera;

        [SerializeField]
        private ARMeshManager _meshManager;

        [Header("Gameboard Configuration")]
        [SerializeField]
        private MeshFilter _walkablePlaneMesh;

        [SerializeField]
        private GameObject _actorPrefab;

        [SerializeField]
        private float _scanInterval = 0.2f;

        [SerializeField]
        private LayerMask _raycastLayerMask;

        [Header("UI")]
        [SerializeField]
        private Button _replaceButton;

        [SerializeField]
        private Text _replaceButtonText;

        [SerializeField]
        private Button _callButton;

#pragma warning restore 0649

        private IGameBoard _gameBoard;
        private AgentConfiguration _agent;
        private GameObject _actor;
        private Surface _surfaceToRender;
        private float _lastScan;
        private bool _isReplacing;
        private bool _isRunning;
        private Coroutine _actorMoveCoroutine;
        private Coroutine _actorJumpCoroutine;
        private List<Waypoint> _lastPath;

        public void StartAR()
        {
            _isRunning = true;
        }

        public void StopAR()
        {
            _meshManager.ClearMeshObjects();
            _walkablePlaneMesh.mesh = null;
            _surfaceToRender = null;

            Destroy(_actor);
            _actor = null;

            if (_actorMoveCoroutine != null)
            {
                StopCoroutine(_actorMoveCoroutine);
                _actorMoveCoroutine = null;
            }

            if (_actorJumpCoroutine != null)
            {
                StopCoroutine(_actorJumpCoroutine);
                _actorJumpCoroutine = null;
            }

            _replaceButtonText.text = "Place";

            _replaceButton.interactable = false;
            _callButton.interactable = false;

            _isReplacing = false;
            _isRunning = false;
            
            ClearBoard();
        }

        private void Awake()
        {
            // Get typical settings for the game board
            var settings = BoardConfiguration.Default;

            // Assign the layer of the mesh
            settings.LayerMask = _raycastLayerMask;

            // Allocate the game board
            _gameBoard = new GameBoard(settings);

            // Create an agent with jumping capabilities
            _agent = AgentConfiguration.CreateJumpingAgent();

            // Allocate mesh to render walkable surfaces
            _walkablePlaneMesh.mesh = new Mesh();
            _walkablePlaneMesh.mesh.MarkDynamic();

            _callButton.interactable = false;
            _replaceButton.interactable = false;
            _replaceButtonText.text = "Place";
        }

        private void OnEnable()
        {
            _replaceButton.onClick.AddListener(ReplaceButton_OnClick);
            _callButton.onClick.AddListener(CallButton_OnClick);
        }

        private void OnDisable()
        {
            _replaceButton.onClick.RemoveListener(ReplaceButton_OnClick);
            _callButton.onClick.RemoveListener(CallButton_OnClick);
        }

        private void Update()
        {
            if (!_isRunning)
                return;
            
            if (_isReplacing)
            {
                HandlePlacement();
            }
            else
            {
                HandleScanning();
            }

            // Render the surface
            if (_surfaceToRender != null)
            {
                _gameBoard.UpdateSurfaceMesh(_surfaceToRender, _walkablePlaneMesh.mesh);
            }
        }

        private void OnDrawGizmos()
        {
          // Visualize navigation paths
          if (_lastPath != null && _actorMoveCoroutine != null)
            for (var i = 0; i < _lastPath.Count; i++)
            {
              var position = _lastPath[i].WorldPosition;
              Gizmos.DrawSphere(position, 0.05f);
              Gizmos.DrawLine
              (
                _lastPath[i].WorldPosition,
                _lastPath[Mathf.Clamp(i + 1, 0, _lastPath.Count - 1)].WorldPosition
              );
            }
          
          // Note: Use the API below to visualize the data structure of the board.
          // Use top-down isometric view in the inspector, make sure gizmos are enabled.
          // _gameBoard?.DrawGizmos(visualizeSpatialTree: true);
        }

        private void HandlePlacement()
        {
          // Use this technique to place an object to a user-defined position.
          // Otherwise, use FindRandomPosition() to try to place the object automatically.
          
          // Get a ray pointing in the user's look direction
          var cameraTransform = _arCamera.transform;
          var ray = new Ray(cameraTransform.position, cameraTransform.forward);

          // Intersect the game board with the ray
          if (_gameBoard.RayCast(ray, out _surfaceToRender, out Vector3 hitPoint))
          {
            // Check whether the object can be fit in the resulting position
            if (_gameBoard.CanFitObject(center: hitPoint, extent: 0.2f))
            {
              _actor.transform.position = hitPoint;
              _replaceButton.interactable = true;
            }
          }
        }

        private void HandleScanning()
        {
          // We should periodically update the game board to let mesh mesh changes accumulate
          // Here, we bind the frequency of our scans to a predefined interval
          if (!(Time.time - _lastScan > _scanInterval))
          {
            return;
          }

          _lastScan = Time.time;
          
          var cameraTransform = _arCamera.transform;
          var playerPosition = cameraTransform.position;
          var playerForward = cameraTransform.forward;
          
          // The GameBoard scans the environment with grid aligned rays.
          // The rays originate from the same level of elevation as the
          // point we specify here. It is important to have this origin
          // above ground level. In this instance, it is at the camera's level.
          var origin = playerPosition +
            // The origin of the scan should be in front of the player.
            Vector3.ProjectOnPlane(playerForward, Vector3.up).normalized;

          // Scan in a 75 cm range from origin
          _gameBoard.Scan(origin, range: 0.75f);

          // Raycast the game board to get the surface the player is looking at
          var ray = new Ray(playerPosition, playerForward);
          _gameBoard.RayCast(ray, out _surfaceToRender, out Vector3 _);

          // Only allow placing the actor if at least one surface is discovered
          _replaceButton.interactable = _gameBoard.NumberOfPlanes > 0;
        }

        private IEnumerator Move(Transform actor, IList<Waypoint> path, float speed = 3.0f)
        {
            var startPosition = actor.position;
            var interval = 0.0f;
            var destIdx = 0;

            while (destIdx < path.Count)
            {
                interval += Time.deltaTime * speed;
                actor.position = Vector3.Lerp(startPosition, path[destIdx].WorldPosition, interval);
                if (Vector3.Distance(actor.position, path[destIdx].WorldPosition) < 0.01f)
                {
                    startPosition = actor.position;
                    interval = 0;
                    destIdx++;

                    // Do we need to jump?
                    if (destIdx < path.Count && path[destIdx].Type == Waypoint.MovementType.SurfaceEntry)
                    {
                        yield return new WaitForSeconds(0.5f);

                        _actorJumpCoroutine = StartCoroutine
                        (
                            Jump(actor, actor.position, path[destIdx].WorldPosition)
                        );

                        yield return _actorJumpCoroutine;

                        _actorJumpCoroutine = null;
                        startPosition = actor.position;
                        destIdx++;
                    }
                }

                yield return null;
            }

            _actorMoveCoroutine = null;
        }

        private IEnumerator Jump(Transform actor, Vector3 from, Vector3 to, float speed = 2.0f)
        {
            var interval = 0.0f;
            var height = Mathf.Max(0.1f, Mathf.Abs(to.y - from.y));
            while (interval < 1.0f)
            {
                interval += Time.deltaTime * speed;
                var p = Vector3.Lerp(from, to, interval);
                actor.position = new Vector3
                (
                    p.x,
                    -4.0f * height * interval * interval +
                    4.0f * height * interval +
                    Mathf.Lerp(from.y, to.y, interval),
                    p.z
                );

                yield return null;
            }

            actor.position = to;
        }

        public void ClearBoard()
        {
            _gameBoard.Clear();
        }

        private void ReplaceButton_OnClick()
        {
            if (_actor == null)
            {
                _actor = Instantiate(_actorPrefab);
            }

            _isReplacing = !_isReplacing;
            _replaceButtonText.text = _isReplacing ? "Done" : "Replace";
            _replaceButton.interactable = !_isReplacing;
            _callButton.interactable = !_isReplacing;
        }

        private void CallButton_OnClick()
        {
          _lastPath = _gameBoard.CalculatePath
            (_actor.transform.position, _arCamera.transform.position, _agent);

            if (_actorMoveCoroutine != null)
                StopCoroutine(_actorMoveCoroutine);

            if (_actorJumpCoroutine != null)
                StopCoroutine(_actorJumpCoroutine);

            _actorMoveCoroutine = StartCoroutine(Move(_actor.transform, _lastPath));
        }
    }
}
// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Configuration;
using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.ReferenceImage;
using Niantic.ARDK.Extensions;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Collections;

using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Niantic.ARDKExamples
{
  // Image Detection example. Shows how to create and use an ARImageDetectionManager, both through
  // the inspector and through code. For the manager created through code, shows how to create 
  // ARReferenceImages both from a byte stream and from a file.
  // Also includes adding and removing an image from a manager at runtime.
  //
  // The expected behavior is that color-coded rectangles will appear over the image if it shows up
  // in the real environment (such as pulled up on a computer monitor). The rectangle will follow if
  // the image moves, but it jumps a few times a second rather than smoothly.
  // For the inspector created manager, a blue rectangle will appear over the image of the crowd.
  // For the code created manager, red and green rectangles will appear over the images of the
  // Niantic yeti and logo.
  // If the detected images are changed (by switching between managers, or by enabling/disabling the
  // yeti) the detected anchors will be cleared.
  //
  // See the "Detecting Images" page in the User Manual for further information on how to optimally
  // detect images and use image anchors.
  public class ImageDetectionExampleManager:
    MonoBehaviour
  {
    // Prefab to spawn on top of detected images.
    [SerializeField]
    private GameObject _plane = null;

    [Header("Image detection managers")]
    [SerializeField]
    private ARImageDetectionManager _inspectorImageDetectionManager;

    [SerializeField]
    private ARImageDetectionManager _codeImageDetectionManager;

    [Header("Images to manually add")]
    // Raw bytes of the jpg image used to test creating an image reference from a local file.
    [SerializeField]
    private TextAsset _filePathImageBytes;

    // Size (meters) of the file path image in physical form.
    [SerializeField]
    private float _filePathImagePhysicalSize;

    // Raw bytes of the jpg image used to test creating an image reference from a byte buffer.
    [SerializeField]
    private TextAsset _byteBufferImageBytes;

    // Size (meters) of the byte buffer image in physical form.
    [SerializeField]
    private float _byteBufferImagePhysicalSize;

    [Header("Controls")]
    // A button that will be configured to enable/disable the manually created ARImageDetectionManager.
    [SerializeField]
    private Button _toggleCodeImageManagerButton;
    private Text _toggleCodeImageManagerButtonText;
    
    // A button that will be configured to enable/disable the automatically created ARImageDetectionManager.
    [SerializeField]
    private Button _toggleInspectorImageManagerButton;
    private Text _toggleInspectorImageManagerButtonText;
    
    // A button that will be configured to enable/disable the Yeti image in the _codeImageDetectionManager
    // (which is the byte buffer image).
    [SerializeField]
    private Button _toggleYetiButton;
    private Text _toggleYetiButtonText;
    // A handle to the yeti image, used to remove and insert it into the _codeImageDetectionManager.
    private IARReferenceImage _yetiImage;

    private Dictionary<Guid, GameObject> _detectedImages = new Dictionary<Guid, GameObject>();

    private bool _yetiImageInImageSet = true;

    private void Start()
    {
      ARSessionFactory.SessionInitialized += SetupSession;
      
      _toggleYetiButton.onClick.AddListener(ToggleYetiImage);
      _toggleYetiButtonText = _toggleYetiButton.GetComponentInChildren<Text>();
      
      _toggleInspectorImageManagerButton.onClick.AddListener(ToggleInspectorImageManager);
      _toggleInspectorImageManagerButtonText = _toggleInspectorImageManagerButton.GetComponentInChildren<Text>();
      
      _toggleCodeImageManagerButton.onClick.AddListener(ToggleCodeImageManager);
      _toggleCodeImageManagerButtonText = _toggleCodeImageManagerButton.GetComponentInChildren<Text>();
      
      SetupCodeImageDetectionManager();
      
      UpdateButtonText();
    }

    private static string BoolText(bool currentCondition)
    {
      return currentCondition ? "Disable" : "Enable";
    }

    private void UpdateButtonText()
    {
      // Set the text of all the buttons based on the current state.
      _toggleYetiButtonText.text = BoolText(_yetiImageInImageSet) + " Yeti Image";

      _toggleInspectorImageManagerButtonText.text =
        BoolText(_inspectorImageDetectionManager.AreFeaturesEnabled) +
        " Inspector Created Manager";
      
      _toggleCodeImageManagerButtonText.text =
        BoolText(_codeImageDetectionManager.AreFeaturesEnabled) +
        " Code Created Manager";
    }

    private void SetupSession(AnyARSessionInitializedArgs arg)
    {
      // Add listeners to all relevant ARSession events.
      var session = arg.Session;
      session.SessionFailed += args => Debug.Log(args.Error);
      session.AnchorsAdded += OnAnchorsAdded;
      session.AnchorsUpdated += OnAnchorsUpdated;
      session.AnchorsRemoved += OnAnchorsRemoved;
    }

    private void SetupCodeImageDetectionManager()
    {
      // For the sake of this example, we're loading the specified asset into a temporary file.
      // In a real application, this could be a file downloaded from the internet and written to
      // the device, or a user selected file.
      var tempFilePath = Path.Combine(Application.temporaryCachePath, "filePathImage.jpg");
      File.WriteAllBytes(tempFilePath, _filePathImageBytes.bytes);

      // Create an ARReferenceImage from the local file path.
      var imageFromPath =
        ARReferenceImageFactory.Create
        (
          "filePathImage",
          tempFilePath,
          _filePathImagePhysicalSize
        );

      // Create an ARReferenceImage from raw bytes. In a real application, these bytes
      // could have been received over the network.
      var rawByteBuffer = _byteBufferImageBytes.bytes;
      _yetiImage =
        ARReferenceImageFactory.Create
        (
          "byteBufferImage",
          rawByteBuffer,
          rawByteBuffer.Length,
          _byteBufferImagePhysicalSize
        );

      // Add both images to the manager.
      _codeImageDetectionManager.AddImage(_yetiImage);
      _codeImageDetectionManager.AddImage(imageFromPath);
    }

    private void ToggleYetiImage()
    {
      // This enables/disables the Yeti image by removing it from the manager.
      // This doesn't do anything to the created GameObject. If the yeti hasn't been detected, no
      // new GameObject will be created. If the yeti has already been detected, the GameObject will
      // remain in place but not update if the yeti image is moved.
      if (_yetiImageInImageSet)
      {
        _codeImageDetectionManager.RemoveImage(_yetiImage);
      }
      else
      {
        _codeImageDetectionManager.AddImage(_yetiImage);
      }

      _yetiImageInImageSet = !_yetiImageInImageSet;
      
      UpdateButtonText();
    }

    private void ToggleInspectorImageManager()
    {
      // Disable the manager, or enable it and disable the other one.
      if (_inspectorImageDetectionManager.AreFeaturesEnabled)
      {
        _inspectorImageDetectionManager.DisableFeatures();
      }
      else
      {
        if (_codeImageDetectionManager.AreFeaturesEnabled)
        {
          _codeImageDetectionManager.DisableFeatures();
        }
        _inspectorImageDetectionManager.EnableFeatures();
      }
      
      UpdateButtonText();
    }

    private void ToggleCodeImageManager()
    {
      // Disable the manager, or enable it and disable the other one.
      if (_codeImageDetectionManager.AreFeaturesEnabled)
      {
        _codeImageDetectionManager.DisableFeatures();
      }
      else
      {
        if (_inspectorImageDetectionManager.AreFeaturesEnabled)
        {
          _inspectorImageDetectionManager.DisableFeatures();
        }
        _codeImageDetectionManager.EnableFeatures();
      }
      
      UpdateButtonText();
    }

    private void OnAnchorsAdded(AnchorsArgs args)
    {
      foreach (var anchor in args.Anchors)
      {
        if (anchor.AnchorType != AnchorType.Image)
          continue;

        var imageAnchor = (IARImageAnchor) anchor;
        var imageName = imageAnchor.ReferenceImage.Name;

        var newPlane = Instantiate(_plane);
        SetPlaneColor(newPlane, imageName);
        _detectedImages[anchor.Identifier] = newPlane;

        UpdatePlaneTransform(imageAnchor);
      }
    }

    static Dictionary<string, Color> _imageColors = new Dictionary<string, Color>
    {
      { "byteBufferImage", Color.red },
      { "filePathImage", Color.green },
      { "crowd", Color.blue },
    };
    
    private void SetPlaneColor(GameObject plane, string imageName)
    {
      var renderer = plane.GetComponentInChildren<MeshRenderer>();
      Color planeColor = Color.black;
      _imageColors.TryGetValue(imageName, out planeColor);
      renderer.material.color = planeColor;
    }

    private void OnAnchorsUpdated(AnchorsArgs args)
    {
      foreach (var anchor in args.Anchors)
      {
        if (!_detectedImages.ContainsKey(anchor.Identifier))
          continue;

        var imageAnchor = anchor as IARImageAnchor;
        UpdatePlaneTransform(imageAnchor);
      }
    }

    private void OnAnchorsRemoved(AnchorsArgs args)
    {
      foreach (var anchor in args.Anchors)
      {
        if (!_detectedImages.ContainsKey(anchor.Identifier))
          continue;

        Destroy(_detectedImages[anchor.Identifier]);
        _detectedImages.Remove(anchor.Identifier);
      }
    }

    private void UpdatePlaneTransform(IARImageAnchor imageAnchor)
    {
      var identifier = imageAnchor.Identifier;

      _detectedImages[identifier].transform.position = imageAnchor.Transform.ToPosition();
      _detectedImages[identifier].transform.rotation = imageAnchor.Transform.ToRotation();

      var localScale = _detectedImages[identifier].transform.localScale;
      localScale.x = imageAnchor.ReferenceImage.PhysicalSize.x;
      localScale.z = imageAnchor.ReferenceImage.PhysicalSize.y;
      _detectedImages[identifier].transform.localScale = localScale;
    }
  }
}

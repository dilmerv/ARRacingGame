// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;

namespace Niantic.ARDK.Utilities
{
  /// Cross-platform class that unifies Unity's mouse and Touch input APIs.
  /// @note
  ///   When run in the Unity Editor, the class will convert mouse input changes into touch input.
  ///   When run natively on a mobile device, the class will simply surface Input.GetTouch,
  ///   Input.touchCount, etc.
  public static class PlatformAgnosticInput
  {
    // (Basically) a state machine used to track mouse drags, persistent touches, etc.
    private class MouseEventBuffer
    {
      public void Update(TouchPhase touchPhase, Vector2 mousePosition)
      {
        if (_lastFrame == Time.frameCount)
          return; // Already updated this frame

        _lastFrame = Time.frameCount;

        _touchPhase = touchPhase;

        switch (touchPhase)
        {
          case TouchPhase.Began:
            _currentDelta = Vector2.zero;
            _priorPosition = mousePosition;
            break;

          default:
            // Movement.
            _currentDelta = mousePosition - _priorPosition;
            _priorPosition = mousePosition;
            if (touchPhase == TouchPhase.Moved)
            {
              _touchPhase =
                (_currentDelta == Vector2.zero)
                  ? TouchPhase.Stationary
                  : TouchPhase.Moved;
            }

            break;
        }
      }

      public Touch GetTouch()
      {
        var touch =
          new Touch
          {
            fingerId = 1,
            phase = _touchPhase,
            position = _priorPosition,
            deltaPosition = _currentDelta
          };

        return touch;
      }

      private TouchPhase _touchPhase;
      private Vector2 _priorPosition;
      private Vector2 _currentDelta;

      private int _lastFrame;
    }

    private static readonly MouseEventBuffer _mouseEventBuffer = new MouseEventBuffer();


    /// The number of touches.
    public static int touchCount
    {
      get
      {
        if (Application.isMobilePlatform)
          return Input.touchCount;

        var m0 = KeyCode.Mouse0;
        return (Input.GetKey(m0) || Input.GetKeyDown(m0) || Input.GetKeyUp(m0)) ? 1 : 0;
      }
    }

    /// Call to obtain the status of a finger touching the screen.
    /// @param index
    ///   The touch input on the device screen. If touchCount is greater than zero, this parameter
    ///   sets which screen touch to check. Use zero to obtain the first screen touch.
    /// @returns Touch details as a struct.
    public static Touch GetTouch(int index)
    {
      return Application.isMobilePlatform ? Input.GetTouch(index) : _TouchFromMouse();
    }

    /// Determines if a specific touch is over any UI raycast targets.
    /// Useful for discounting screen touches before processing them as gestures.
    public static bool IsTouchOverUIObject(this Touch touch)
    {
      var eventDataCurrentPosition =
        new PointerEventData(EventSystem.current)
        {
          position = new Vector2(touch.position.x, touch.position.y)
        };

      var results = new List<RaycastResult>();
      EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
      return results.Count > 0;
    }

    private static Touch _TouchFromMouse()
    {
      var m0 = KeyCode.Mouse0;
      var touch = new Touch();

      // Send different state changes depending on the Mouse Click state...
      if (Input.GetKeyDown(m0))
      {
        _mouseEventBuffer.Update(TouchPhase.Began, Input.mousePosition);
        touch = _mouseEventBuffer.GetTouch();
      }
      else if (Input.GetKeyUp(m0))
      {
        _mouseEventBuffer.Update(TouchPhase.Ended, Input.mousePosition);
        touch = _mouseEventBuffer.GetTouch();
      }
      else if (Input.GetKey(m0))
      {
        _mouseEventBuffer.Update(TouchPhase.Moved, Input.mousePosition);
        touch = _mouseEventBuffer.GetTouch();
      }
      else
      {
        touch.phase = TouchPhase.Canceled;
      }

      return touch;
    }
  }
}

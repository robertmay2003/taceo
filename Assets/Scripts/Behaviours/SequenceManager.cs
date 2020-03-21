﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lean.Touch;
using TMPro;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Util;
using Random = UnityEngine.Random;

public class SequenceManager : MonoBehaviour
{

	[System.Serializable]
	public class Sequence
	{

		public enum Gesture
		{
			Tap,
			Long,
			SwipeVertical,
			SwipeHorizontal,
		}

		private List<Gesture> _gestures = new List<Gesture>();

		public Gesture[] Gestures
		{
			get => _gestures.ToArray();
		}

		public Sequence()
		{
			_gestures = new List<Gesture>();
		}

		public Sequence(List<Gesture> list)
		{
			_gestures = list;
		}

		public void AddGesture()
		{
			int values = Enum.GetValues(typeof(Gesture)).Length;
			_gestures.Add((Gesture)Random.Range(0, values));
		}

	}

	[System.Serializable]
	public struct TapVibration
	{
		public Sequence.Gesture gesture;
		public VibrationData vibration;
	}

	public List<TapVibration> vibrations = new List<TapVibration>();
	public VibrationData roundStart;
	public VibrationData gameOver;

	public Sequence sequence;

	public TMP_Text debugText;

	private int _sequenceIndex = 0;

	private float _inputStart;
	private bool _awaitingInput = false;

	private float _vibrationTime;
	private AnimationCurve _vibrationCurve = AnimationCurve.Constant(0, 0, 0);

	private MainMenu.AppState _state;

	// Start is called before the first frame update
    void Start()
    {
	    _state = MainMenu.AppState.MainMenu;

	    LeanTouch.OnFingerTap += HandleTap;
	    LeanTouch.OnFingerUp += HandleHold;
	    LeanTouch.OnFingerSwipe += HandleSwipe;
    }

    // Update is called once per frame
    void Update()
    {
	    // On state change
	    if (_state != MainMenu.state)
	    {
		    _state = MainMenu.state;
		    switch (_state)
		    {
			    case MainMenu.AppState.Game:
					StartGame();
					break;
			    default:
				    Debug.Log(_state);
				    break;
		    }
	    }

	    // Update logo
	    float t = Time.time - _vibrationTime;
	    if (t < _vibrationCurve.keys.Last().time)
	    {
		    LogoBall.vibrationIntensity = _vibrationCurve.Evaluate(t);
	    }
	    else
	    {
		    LogoBall.vibrationIntensity = 0.0f;
	    }
    }

    private void StartGame()
    {
	    StartCoroutine(StartRound());
    }

    private IEnumerator StartRound()
    {
	    _awaitingInput = false;
	    _sequenceIndex = 0;

	    yield return new WaitForSeconds(1);
	    debugText.text = "Round Start";
	    roundStart.Start();
	    yield return new WaitForSeconds(2);
	    sequence.AddGesture();
	    foreach (Sequence.Gesture gesture in sequence.Gestures)
	    {
		    TapVibration vibration = TapVibrationFromGesture(gesture);
		    debugText.text = vibration.gesture.ToString();
		    vibration.vibration.Start();
		    LogoVibration(vibration.vibration);

		    yield return new WaitForSeconds(1.5f);
	    }

	    debugText.text = "Your turn";
	    _awaitingInput = true;
	    roundStart.Start();
    }

    private TapVibration TapVibrationFromGesture(Sequence.Gesture gesture)
    {
	    TapVibration vibration = vibrations.Where(v => v.gesture == gesture).ToArray()[0];
	    return vibration;
    }

    private void CheckGesture(Sequence.Gesture gesture)
    {
	    Debug.Log(gesture.ToString());
	    if (gesture == sequence.Gestures[_sequenceIndex])
	    {
		    TapVibration vibration = TapVibrationFromGesture(gesture);
		    debugText.text = vibration.gesture.ToString();
		    vibration.vibration.Start();

		    _sequenceIndex += 1;

		    // If round is over
		    if (_sequenceIndex == sequence.Gestures.Length)
		    {
			    _awaitingInput = false;
			    StartCoroutine(StartRound());
		    }
	    }
	    else
	    {
		    // Game is over
		    _awaitingInput = false;
		    _inputStart = Time.time;

		    debugText.text = "Game Over";
		    MainMenu.state = MainMenu.AppState.GameOver;
		    gameOver.Start();
	    }
    }

    private void HandleTap(LeanFinger finger)
    {
	    switch (_state)
	    {
		    case MainMenu.AppState.Game:
			    if (_awaitingInput)
			    {
					CheckGesture(Sequence.Gesture.Tap);
			    }

			    break;
		    default:
			    Debug.Log(_state);
			    break;
	    }
    }

    private void HandleHold(LeanFinger finger)
    {
	    switch (_state)
	    {
		    case MainMenu.AppState.Game:
			    if (_awaitingInput)
			    {
				    // If longer than tap, not a swipe, and younger than the beginning of input
				    if (finger.Old && !finger.Swipe && finger.Age < Time.time - _inputStart)
				    {
					    CheckGesture(Sequence.Gesture.Long);
				    }
			    }

			    break;
		    default:
			    Debug.Log(_state);
			    break;
	    }
    }

    private void HandleSwipe(LeanFinger finger)
    {
	    switch (_state)
	    {
		    case MainMenu.AppState.Game:
			    // Make sure swipe did not begin before input
			    if (_awaitingInput && finger.Age < Time.time - _inputStart)
			    {
				    float angle = Mathf.Atan2(finger.SwipeScaledDelta.y, finger.SwipeScaledDelta.x);
				    Debug.Log($"Vector: {finger.SwipeScaledDelta}, Angle: {angle}");
				    Debug.Log($"Cosine: {Mathf.Cos(angle)}, Sine: {Mathf.Sin(angle)}");
				    if (Mathf.Cos(angle) > 0.8)
				    {
					    CheckGesture(Sequence.Gesture.SwipeHorizontal);
				    }
				    else if (Mathf.Sin(angle) > 0.8)
				    {
					    CheckGesture(Sequence.Gesture.SwipeVertical);
				    }
			    }

			    break;
		    default:
			    Debug.Log(_state);
			    break;
	    }
    }

    private void LogoVibration(VibrationData vibration)
    {
	    _vibrationTime = Time.time;
	    _vibrationCurve = vibration.intensityCurve;
    }
}

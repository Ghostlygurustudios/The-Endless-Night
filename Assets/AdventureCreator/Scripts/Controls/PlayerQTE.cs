/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2016
 *	
 *	"PlayerQTE.cs"
 * 
 *	This script handles the processing of quick-time events
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/**
	 * This script handles the state, input and progress of Quick Time Events (QTEs).
	 * It should be attached to the GameEngine prefab.
	 */
	#if !(UNITY_4_6 || UNITY_4_7 || UNITY_5_0)
	[HelpURL("http://www.adventurecreator.org/scripting-guide/class_a_c_1_1_player_q_t_e.html")]
	#endif
	public class PlayerQTE : MonoBehaviour
	{

		private QTEState qteState = QTEState.None;
		private QTEType qteType = QTEType.SingleKeypress;
		
		private string inputName;
		private Animator animator;
		private bool wrongKeyFails;

		private float holdDuration;
		private float cooldownTime;
		private int targetPresses;
		private bool doCooldown;

		private float progress;
		private int numPresses;
		private float startTime;
		private float endTime;
		private float lastPressTime;
		private bool canMash;


		public void OnAwake ()
		{
			SkipQTE ();
		}


		/**
		 * <summary>Gets the current QTE state (None, Win, Lose)</summary>
		 * <returns>The current QTE state (None, Win, Lose)</returns>
		 */
		public QTEState GetState ()
		{
			return qteState;
		}


		/**
		 * Automatically wins the current QTE.
		 */
		public void SkipQTE ()
		{
			endTime = 0f;
			qteState = QTEState.Win;
		}


		/**
		 * <summary>Begins a QTE that involves a single key being pressed to win.</summary>
		 * <param name = "_inputName">The name of the input button that must be pressed to win</param>
		 * <param name = "_duration">The duration, in seconds, that the QTE lasts</param>
		 * <param name = "_animator">An Animator that will be manipulated if it has "Win" and "Lose" states</param>
		 * <param name = "_wrongKeyFails">If True, then pressing any key other than _inputName will instantly fail the QTE</param>
		 */
		public void StartSinglePressQTE (string _inputName, float _duration, Animator _animator = null, bool _wrongKeyFails = false)
		{
			if (_inputName == "" || _duration <= 0f)
			{
				return;
			}

			Setup (QTEType.SingleKeypress, _inputName, _duration, _animator, _wrongKeyFails);
		}


		/**
		 * <summary>Begins a QTE that involves a single key being held down to win.</summary>
		 * <param name = "_inputName">The name of the input button that must be held down to win</param>
		 * <param name = "_duration">The duration, in seconds, that the QTE lasts</param>
		 * <param name = "_holdDuration">The duration, in seconds, that the key must be held down for</param>
		 * <param name = "_animator">An Animator that will be manipulated if it has "Win" and "Lose" states, and a "Held" trigger</param>
		 * <param name = "_wrongKeyFails">If True, then pressing any key other than _inputName will instantly fail the QTE</param>
		 */
		public void StartHoldKeyQTE (string _inputName, float _duration, float _holdDuration, Animator _animator = null, bool _wrongKeyFails = false)
		{
			if (_inputName == "" || _duration <= 0f)
			{
				return;
			}

			holdDuration = _holdDuration;
			Setup (QTEType.HoldKey, _inputName, _duration, _animator, _wrongKeyFails);
		}


		/**
		 * <summary>Begins a QTE that involves a single key being pressed repeatedly to win.</summary>
		 * <param name = "_inputName">The name of the input button that must be pressed repeatedly to win</param>
		 * <param name = "_duration">The duration, in seconds, that the QTE lasts</param>
		 * <param name = "_targetPresses">The number of times that the key must be pressed to win</param>
		 * <param name = "_doCooldown">If True, then the number of registered key-presses will decrease over time</param>
		 * <param name = "_cooldownTime">The cooldown time, if _doCooldown = True</param>
		 * <param name = "_animator">An Animator that will be manipulated if it has "Hit", "Win" and "Lose" states</param>
		 * <param name = "_wrongKeyFails">If True, then pressing any key other than _inputName will instantly fail the QTE</param>
		 */
		public void StartButtonMashQTE (string _inputName, float _duration, int _targetPresses, bool _doCooldown, float _cooldownTime, Animator _animator = null, bool _wrongKeyFails = false)
		{
			if (_inputName == "" || _duration <= 0f)
			{
				return;
			}

			targetPresses = _targetPresses;
			doCooldown = _doCooldown;
			cooldownTime = _cooldownTime;

			Setup (QTEType.ButtonMash, _inputName, _duration, _animator, _wrongKeyFails);
		}


		private void Setup (QTEType _qteType, string _inputName, float _duration, Animator _animator, bool _wrongKeyFails)
		{
			qteType = _qteType;
			qteState = QTEState.None;

			progress = 0f;
			inputName = _inputName;
			animator = _animator;
			wrongKeyFails = _wrongKeyFails;
			numPresses = 0;
			startTime = Time.time;
			lastPressTime = 0f;
			endTime = Time.time + _duration;
		}


		/**
		 * <summary>Gets the time factor remaining in the current QTE, as a decimal.</summary>
		 * <returns>The time factor remaining in the current QTE, as a decimal</returns>
		 */
		public float GetRemainingTimeFactor ()
		{
			if (endTime == 0f || Time.time <= startTime)
			{
				return 1f;
			}

			if (Time.time >= endTime)
			{
				return 0f;
			}
			return (1f - (Time.time - startTime) / (endTime - startTime));
		}


		/**
		 * <summary>Gets the progress made towards completing the current QTE, as a decimal.</summary>
		 * <returns>The progress made towards competing the current QTE, as a decimal</returns>
		 */
		public float GetProgress ()
		{
			if (qteState == QTEState.Win)
			{
				progress = 1f;
			}
			else if (qteState == QTEState.Lose)
			{
				progress = 0f;
			}
			else if (endTime > 0f)
			{
				if (qteType == QTEType.HoldKey)
				{
					if (lastPressTime == 0f)
					{
						progress = 0f;
					}
					else
					{
						progress = ((Time.time - lastPressTime) / holdDuration);
					}
				}
				else if (qteType == QTEType.ButtonMash)
				{
					progress = (float) numPresses / (float) targetPresses;
				}
			}

			return progress;
		}


		/**
		 * <summary>Checks if a QTE sequence is currently active.</summary>
		 * <returns>True if a QTE sequence is currently active.</returns>
		 */
		public bool QTEIsActive ()
		{
			if (endTime > 0f)
			{
				return true;
			}
			return false;
		}

		/**
		 * Updates the current QTE. This is called every frame by StateHandler.
		 */
		public void UpdateQTE ()
		{
			if (endTime == 0f)
			{
				return;
			}

			if (Time.time > endTime)
			{
				Lose ();
				return;
			}
			
			if (qteType == QTEType.SingleKeypress)
			{
				if (KickStarter.playerInput.InputGetButtonDown (inputName))
				{
					Win ();
					return;
				}
				else if (wrongKeyFails && KickStarter.playerInput.InputAnyKey () && KickStarter.playerInput.GetMouseState () == MouseState.Normal)
				{
					Lose ();
					return;
				}
			}
			else if (qteType == QTEType.ButtonMash)
			{
				if (KickStarter.playerInput.InputGetButtonDown (inputName))
				{
					if (canMash)
					{
						numPresses ++;
						lastPressTime = Time.time;
						if (animator)
						{
							animator.Play ("Hit", 0, 0f);
						}
						canMash = false;
					}
				}
				else
				{
					canMash = true;

					if (doCooldown)
					{
						if (lastPressTime > 0f && Time.time > lastPressTime + cooldownTime)
						{
							numPresses --;
							lastPressTime = Time.time;
						}
					}
				}

				if (KickStarter.playerInput.InputGetButtonDown (inputName)) {}
				else if (wrongKeyFails && Input.anyKeyDown)
				{
					Lose ();
					return;
				}

				
				if (numPresses < 0)
				{
					numPresses = 0;
				}
				
				if (numPresses >= targetPresses)
				{
					Win ();
					return;
				}
			}
			else if (qteType == QTEType.HoldKey)
			{
				if (KickStarter.playerInput.InputGetButton (inputName))
				{
					if (lastPressTime == 0f)
					{
						lastPressTime = Time.time;
					}
					else if (Time.time > lastPressTime + holdDuration)
					{
						Win ();
						return;
					}
				}
				else if (wrongKeyFails && Input.anyKey)
				{
					Lose ();
					return;
				}
				else
				{
					lastPressTime = 0f;
				}

				if (animator)
				{
					if (lastPressTime == 0f)
					{
						animator.SetBool ("Held", false);
					}
					else
					{
						animator.SetBool ("Held", true);
					}
				}
			}
		}


		private void Win ()
		{
			if (animator)
			{
				animator.Play ("Win");
			}
			qteState = QTEState.Win;
			endTime = 0f;
		}


		private void Lose ()
		{
			qteState = QTEState.Lose;
			endTime = 0f;
			if (animator)
			{
				animator.Play ("Lose");
			}
		}

	}

}
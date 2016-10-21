using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace AC
{

	[CustomEditor (typeof (AC.Char))]
	public class CharEditor : Editor
	{

		private Expression expressionToAffect;


		public override void OnInspectorGUI ()
		{
			EditorGUILayout.HelpBox ("This component should not be used directly - use Player or NPC instead.", MessageType.Warning);
		}


		protected void SharedGUIOne (AC.Char _target)
		{
			_target.GetAnimEngine ();

			EditorGUILayout.BeginVertical ("Button");
				EditorGUILayout.LabelField ("Animation settings:", EditorStyles.boldLabel);
				_target.animationEngine = (AnimationEngine) EditorGUILayout.EnumPopup ("Animation engine:", _target.animationEngine);
				if (_target.animationEngine == AnimationEngine.Sprites2DToolkit && !tk2DIntegration.IsDefinePresent ())
				{
					EditorGUILayout.HelpBox ("The 'tk2DIsPresent' preprocessor define must be declared in the\ntk2DIntegration.cs script. Please open it and follow instructions.", MessageType.Warning);
				}
				else if (_target.animationEngine == AnimationEngine.Custom)
				{
					_target.customAnimationClass = EditorGUILayout.TextField ("Script name:", _target.customAnimationClass);
				}
				_target.motionControl = (MotionControl) EditorGUILayout.EnumPopup ("Motion control:", _target.motionControl);
			EditorGUILayout.EndVertical ();

			_target.GetAnimEngine ().CharSettingsGUI ();

			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Movement settings:", EditorStyles.boldLabel);

			if (_target.GetMotionControl () == MotionControl.Automatic)
			{
	 			_target.walkSpeedScale = EditorGUILayout.FloatField ("Walk speed scale:", _target.walkSpeedScale);
				_target.runSpeedScale = EditorGUILayout.FloatField ("Run speed scale:", _target.runSpeedScale);
				_target.acceleration = EditorGUILayout.FloatField ("Acceleration:", _target.acceleration);
				_target.deceleration = EditorGUILayout.FloatField ("Deceleration:", _target.deceleration);
				_target.runDistanceThreshold = EditorGUILayout.FloatField ("Minimum run distance:", _target.runDistanceThreshold);
			}
			if (_target.GetMotionControl () != MotionControl.Manual)
			{
				_target.turnSpeed = EditorGUILayout.FloatField ("Turn speed:", _target.turnSpeed);
			}
			_target.turnBeforeWalking = EditorGUILayout.Toggle ("Turn before walking?", _target.turnBeforeWalking);
			EditorGUILayout.EndVertical ();
		}


		protected void SharedGUITwo (AC.Char _target)
		{
			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Rigidbody settings:", EditorStyles.boldLabel);
			_target.ignoreGravity = EditorGUILayout.Toggle ("Ignore gravity?", _target.ignoreGravity);
			_target.freezeRigidbodyWhenIdle = EditorGUILayout.Toggle ("Freeze when Idle?", _target.freezeRigidbodyWhenIdle);
			EditorGUILayout.EndVertical ();
			
			
			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Audio clips:", EditorStyles.boldLabel);
		
			_target.walkSound = (AudioClip) EditorGUILayout.ObjectField ("Walk sound:", _target.walkSound, typeof (AudioClip), false);
			_target.runSound = (AudioClip) EditorGUILayout.ObjectField ("Run sound:", _target.runSound, typeof (AudioClip), false);
			if (AdvGame.GetReferences ().speechManager != null && AdvGame.GetReferences ().speechManager.scrollSubtitles)
			{
				_target.textScrollClip = (AudioClip) EditorGUILayout.ObjectField ("Text scroll override:", _target.textScrollClip, typeof (AudioClip), false);
			}
			_target.soundChild = (Sound) EditorGUILayout.ObjectField ("SFX Sound child:", _target.soundChild, typeof (Sound), true);
			_target.speechAudioSource = (AudioSource) EditorGUILayout.ObjectField ("Speech AudioSource:", _target.speechAudioSource, typeof (AudioSource), true);
			EditorGUILayout.EndVertical ();
			
			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Dialogue settings:", EditorStyles.boldLabel);

			_target.speechColor = EditorGUILayout.ColorField ("Speech text colour:", _target.speechColor);
			_target.speechLabel = EditorGUILayout.TextField ("Speaker label:", _target.speechLabel);
			_target.speechMenuPlacement = (Transform) EditorGUILayout.ObjectField ("Speech menu placement child:", _target.speechMenuPlacement, typeof (Transform), true);
			if (_target.useExpressions)
			{
				EditorGUILayout.LabelField ("Default portrait graphic:");
			}
			else
			{
				EditorGUILayout.LabelField ("Portrait graphic:");
			}
			_target.portraitIcon.ShowGUI (false);

			_target.useExpressions = EditorGUILayout.Toggle ("Use expressions?", _target.useExpressions);
			if (_target.useExpressions)
			{
				EditorGUILayout.Space ();
				EditorGUILayout.BeginVertical ("Button");
				for (int i=0; i<_target.expressions.Count; i++)
				{
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Expression #" + _target.expressions[i].ID.ToString (), EditorStyles.boldLabel);
					if (GUILayout.Button (Resource.CogIcon, GUILayout.Width (20f), GUILayout.Height (15f)))
					{
						ExpressionSideMenu (_target, i);
					}
					EditorGUILayout.EndHorizontal ();
					_target.expressions[i].ShowGUI ();
				}

				if (GUILayout.Button ("Add new expression"))
				{
					_target.expressions.Add (new Expression (GetExpressionIDArray (_target.expressions)));
				}
				EditorGUILayout.EndVertical ();
			}

			EditorGUILayout.EndVertical ();
		}


		private int[] GetExpressionIDArray (List<Expression> expressions)
		{
			List<int> idArray = new List<int>();
			if (expressions != null)
			{
				foreach (Expression expression in expressions)
				{
					idArray.Add (expression.ID);
				}
			}
			idArray.Sort ();
			return idArray.ToArray ();
		}


		private void ExpressionSideMenu (AC.Char _target, int i)
		{
			expressionToAffect = _target.expressions[i];
			GenericMenu menu = new GenericMenu ();
			
			menu.AddItem (new GUIContent ("Insert after"), false, Callback, "Insert after");
			if (_target.expressions.Count > 1)
			{
				menu.AddItem (new GUIContent ("Delete"), false, Callback, "Delete");
			}
			if (i > 0)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move up"), false, Callback, "Move up");
			}
			if (i < _target.expressions.Count-1)
			{
				menu.AddItem (new GUIContent ("Re-arrange/Move down"), false, Callback, "Move down");
			}
			
			menu.ShowAsContext ();
		}


		private void Callback (object obj)
		{
			AC.Char t = (AC.Char) target;
			ModifyAction (t, expressionToAffect, obj.ToString ());
			EditorUtility.SetDirty (t);
		}
		
		
		private void ModifyAction (AC.Char _target, Expression _expression, string callback)
		{
			int i = -1;
			if (_expression != null && _target.expressions.IndexOf (_expression) > -1)
			{
				i = _target.expressions.IndexOf (_expression);
			}
			
			switch (callback)
			{
			case "Insert after":
				Undo.RecordObject (_target, "Create expression");
				_target.expressions.Insert (i+1, new Expression (GetExpressionIDArray (_target.expressions)));
				break;
				
			case "Delete":
				Undo.RecordObject (_target, "Delete expression");
				_target.expressions.Remove (_expression);
				break;
				
			case "Move up":
				Undo.RecordObject (_target, "Move expression up");
				_target.expressions.Remove (_expression);
				_target.expressions.Insert (i-1, _expression);
				break;
				
			case "Move down":
				Undo.RecordObject (_target, "Move expression down");
				_target.expressions.Remove (_expression);
				_target.expressions.Insert (i+1, _expression);
				break;
			}

		}

	}

}
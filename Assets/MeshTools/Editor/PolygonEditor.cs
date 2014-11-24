using System.Collections;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Polygon))]
public class PolygonEditor : Editor {
	
	bool liveEdit = true;
	SerializedProperty vertProp;
	
	void OnEnable() {
		vertProp = serializedObject.FindProperty("verts");
	}
	
	
	public override void OnInspectorGUI()
	{
		var edit = EditorGUILayout.Toggle("Live Edit?", liveEdit);
		if (edit ^ liveEdit) {
			liveEdit = edit;
			SceneView.RepaintAll();
		}
		DrawDefaultInspector();
	}
	
	void OnSceneGUI() {
		if (!liveEdit) { return; }
		
		var xf = (target as Polygon).GetComponent<Transform>();
		var q = xf.rotation;
		serializedObject.Update();
		for(int i=0, len=vertProp.arraySize; i<len; ++i) {
			var elem = vertProp.GetArrayElementAtIndex(i);
			elem.vector3Value = xf.InverseTransformPoint(
				//Handles.PositionHandle(xf.TransformPoint(elem.vector3Value), q)
				Handles.FreeMoveHandle(
					xf.TransformPoint(elem.vector3Value), 
					q, 
					0.5f, 
					Vector3.zero,
					Handles.DotCap
				)
			);
		}
		if (GUI.changed) {
			EditorUtility.SetDirty(target);
		}
		serializedObject.ApplyModifiedProperties();
	}
	

}

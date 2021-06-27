using System.Text;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;


namespace Moenen.Standard {

	public class MoenenTools {



		private static long PrevDoTheThingTime = 0;
		private static int DoTheTingCombo = 0;


		[InitializeOnLoadMethod]
		public static void EditorInit () {
			SceneView.duringSceneGui += (sceneView) => {
				// Moenen's Scene Camera
				if (!sceneView.in2DMode) {
					switch (Event.current.type) {
						case EventType.MouseDrag:
							if (Event.current.button == 1) {
								// Mosue Right Drag
								if (!Event.current.alt) {
									// View Rotate
									Vector2 del = Event.current.delta * 0.2f;
									float angle = sceneView.camera.transform.rotation.eulerAngles.x + del.y;
									angle = angle > 89 && angle < 180 ? 89 : angle;
									angle = angle > 180 && angle < 271 ? 271 : angle;
									sceneView.LookAt(
										sceneView.pivot,
										Quaternion.Euler(
											angle,
											sceneView.camera.transform.rotation.eulerAngles.y + del.x,
											0f
										),
										sceneView.size,
										sceneView.orthographic,
										true
									);
									Event.current.Use();
								}
							}
							break;
					}
				}
			};
		}


		// & alt   % ctrl   # Shift
		[MenuItem("Tools/Do the Thing _F5")]
		public static void ClearAndReStage () {

			// Clear Console
			var assembly = Assembly.GetAssembly(typeof(ActiveEditorTracker));
			var type = assembly.GetType("UnityEditorInternal.LogEntries");
			if (type == null) {
				type = assembly.GetType("UnityEditor.LogEntries");
			}
			var method = type.GetMethod("Clear");
			method.Invoke(new object(), null);

			// Combo
			long time = System.DateTime.Now.Ticks;
			if (time - PrevDoTheThingTime < 5000000) {
				if (DoTheTingCombo == 0) {
					// Save
					if (!EditorApplication.isPlaying) {
						EditorSceneManager.SaveOpenScenes();
					}
				} else if (DoTheTingCombo == 1) {
					// Deselect
					Selection.activeObject = null;
				}
				DoTheTingCombo++;
			} else {
				DoTheTingCombo = 0;
			}
			PrevDoTheThingTime = time;

		}


		[MenuItem("Assets/Create/Package Json", priority = 99)]
		public static void CreatePackageJson () {
			string rootPath = "Assets";
			string packageName = "Package";
			if (Selection.activeObject != null) {
				string selectionPath = AssetDatabase.GetAssetPath(Selection.activeObject);
				if (!string.IsNullOrEmpty(selectionPath)) {
					rootPath = Util.PathIsDirectory(selectionPath) ?
						selectionPath :
						Util.GetParentPath(selectionPath);
					string rootName = Util.GetNameWithoutExtension(rootPath);
					if (rootName != "Assets") {
						packageName = rootName.Replace(" ", "");
					}
				}
			}
			string[] uVers = Application.unityVersion.Split('.');
			var builder = new StringBuilder();
			builder.AppendLine("{");
			builder.AppendLine(@$"  ""name"": ""com.{Application.companyName.ToLower()}.{packageName.ToLower()}"", ");
			builder.AppendLine(@$"  ""displayName"": ""{packageName}"",");
			builder.AppendLine(@"  ""version"": ""1.0.0"",");
			builder.AppendLine(@$"  ""unity"": ""{uVers[0]}.{uVers[1]}"",");
			builder.AppendLine(@"  ""description"": """",");
			builder.AppendLine(@"  ""type"": """",");
			builder.AppendLine(@"  ""hideInEditor"": false,");
			builder.AppendLine(@"  ""dependencies"": { }");
			builder.AppendLine("}");
			Util.TextToFile(
				builder.ToString(),
				EditorUtil.FixedRelativePath(Util.CombinePaths(rootPath, "package.json"))
			);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}


	}
}
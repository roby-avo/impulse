using UnityEditor;
namespace Playground.Editor {
public class VisualModelImporter:AssetPostprocessor {
 void OnPreprocessModel(){if(!assetPath.StartsWith("Assets/Resources/Visuals/"))return;var importer=(ModelImporter)assetImporter;importer.isReadable=true;importer.importAnimation=false;importer.addCollider=false;importer.importNormals=ModelImporterNormals.Import;}
}
}

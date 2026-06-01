@tool
extends EditorScenePostImport

func _post_import(scene: Node) -> Object:
  configure_mesh_layers(scene)
  return scene

func configure_mesh_layers(node: Node) -> void:
  if node is MeshInstance3D or node is ImporterMeshInstance3D:
    node.layers = 2

  for child in node.get_children():
    configure_mesh_layers(child)

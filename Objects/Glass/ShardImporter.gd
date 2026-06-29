@tool
extends EditorScenePostImport

func _post_import(scene: Node) -> Object:
  if scene is not Node3D:
    return scene

  for child in scene.get_children():
    if child is not RigidBody3D:
      continue

    child.collision_layer = 16
    child.collision_mask = 31
    child.mass = 0.1
    child.gravity_scale = 1.5
  
  return scene

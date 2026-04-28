@tool
extends Node
class_name GunLoader

@export var bake_now: bool = false:
	set(value):
		if value:
			bake()

@export var target_res: GunData
@export var bullet_spawner_local_pivot: Node3D

@export_group("Pivots")
@export var default_pivot: Node3D
@export var right_lean_pivot: Node3D
@export var left_lean_pivot: Node3D
@export var aim_pivot: Node3D
@export var right_lean_aim_pivot: Node3D
@export var left_lean_aim_pivot: Node3D
@export var crouch_pivot: Node3D
@export var crouch_right_lean_pivot: Node3D
@export var crouch_left_lean_pivot: Node3D
@export var run_pivot: Node3D
@export var back_run_pivot: Node3D
@export var heap_aim_pivot: Node3D
@export var slide_pivot: Node3D
@export var near_wall_pivot: Node3D

func bake() -> void:
	bake_now = false
	
	if not target_res:
		push_error("GunLoader: No Target Resource or Gun assigned!")
		return

	if bullet_spawner_local_pivot:
		target_res.BulletSpawnerPos = bullet_spawner_local_pivot.position
	if default_pivot:
		target_res.DefaultPos = default_pivot.position
	if right_lean_pivot:
		target_res.RightLeanPos = right_lean_pivot.position
	if left_lean_pivot:
		target_res.LeftLeanPos = left_lean_pivot.position
	if aim_pivot:
		target_res.AimPos = aim_pivot.position
	if right_lean_aim_pivot:
		target_res.RightLeanAimPos = right_lean_aim_pivot.position
	if left_lean_aim_pivot:
		target_res.LeftLeanAimPos = left_lean_aim_pivot.position
	if crouch_pivot:
		target_res.CrouchPos = crouch_pivot.position
	if crouch_right_lean_pivot:
		target_res.CrouchRightLeanPos = crouch_right_lean_pivot.position
	if crouch_left_lean_pivot:
		target_res.CrouchLeftLeanPos = crouch_left_lean_pivot.position
	if slide_pivot:
		target_res.SlidePos = slide_pivot.position

	if run_pivot:
		target_res.RunPos = run_pivot.position
		target_res.RunOrient = run_pivot.quaternion
	if back_run_pivot:
		target_res.BackRunPos = back_run_pivot.position
		target_res.BackRunOrient = back_run_pivot.quaternion
	if heap_aim_pivot:
		target_res.HeapAimPos = heap_aim_pivot.position
		target_res.HeapAimOrient = heap_aim_pivot.quaternion
	if near_wall_pivot:
		target_res.NearWallPos = near_wall_pivot.position
		target_res.NearWallRot = near_wall_pivot.rotation

	var err_code = ResourceSaver.save(target_res, target_res.resource_path)
	if err_code == OK:
		print("GunLoader: Successfully baked to ", target_res.resource_path)
	else:
		push_error("GunLoader: Failed to save resource! Error code: ", err_code)

﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeypointField {

	public List<Vector3> Keypoints = new List<Vector3>();
	public Vector3[] Positions = new Vector3[0];
	public Vector3[] Gradients = new Vector3[0];

	private Actor Actor;
	private float Radius;
	private LayerMask Mask;

	private bool Constant;

	public KeypointField(Actor actor, float radius, LayerMask mask, bool constant=false) {
		Actor = actor;
		Radius = radius;
		Mask = mask;
		Constant = constant;
	}

	private float GetRadius(int index) {
		if(Constant) {
			return 0.75f;
		} else {
			float length = Actor.Bones[index].GetLength();
			for(int i=0; i<Actor.Bones[index].Childs.Length; i++) {
				length += Actor.Bones[index].GetChild(i).GetLength();
			}
			length /= Actor.Bones[index].Childs.Length + 1;
			return Radius*length;
		}
	}

	public void Sense() {
		Keypoints.Clear();
		Positions = new Vector3[Actor.Bones.Length];
		Gradients = new Vector3[Actor.Bones.Length];
		LayerMask groundMask = LayerMask.GetMask("Ground");
		for(int i=0; i<Actor.Bones.Length; i++) {

			Positions[i] = Actor.Bones[i].Transform.position;
			List<Vector3> points = new List<Vector3>();
			List<Vector3> origins = new List<Vector3>();

			Collider[] colliders = Physics.OverlapSphere(Positions[i], GetRadius(i), Mask);
			for(int j=0; j<colliders.Length; j++) {
				if((LayerMask.LayerToName(colliders[j].gameObject.layer) == "Objects") && !(colliders[j] is MeshCollider && !((MeshCollider)colliders[j]).convex)) {
					points.Add(colliders[j].ClosestPoint(Positions[i]));
					origins.Add(colliders[j].bounds.center);
				}
			}
			for(int j=0; j<colliders.Length; j++) {
				if((LayerMask.LayerToName(colliders[j].gameObject.layer) == "Ground") && !(colliders[j] is MeshCollider && !((MeshCollider)colliders[j]).convex)) {
					points.Add(colliders[j].ClosestPoint(Positions[i]));
					Vector3 origin = Utility.ProjectGround(Positions[i], groundMask);
					origin.y = Mathf.Min(origin.y, Positions[i].y - 0.001f);
					origins.Add(origin);
				}
			}

			Vector3 gradient = Vector3.zero;
			for(int j=0; j<points.Count; j++) {
				float w = 1f - Vector3.Distance(Positions[i], points[j]) / GetRadius(i);
				Vector3 v = Utility.Interpolate((points[j] - Positions[i]).normalized, (origins[j] - Positions[i]).normalized, w * w);
				gradient += w * GetRadius(i) * v;
			}
			gradient = Vector3.ClampMagnitude(gradient, GetRadius(i));
			Gradients[i] = gradient;
			Keypoints.AddRange(points);
		}
	}

	public void Draw() {
		UltiDraw.Begin();
		for(int i=0; i<Keypoints.Count; i++) {
			UltiDraw.DrawSphere(Keypoints[i], Quaternion.identity, 0.025f, UltiDraw.Red);
		}
		for(int i=0; i<Actor.Bones.Length; i++) {
			UltiDraw.DrawWireSphere(Actor.Bones[i].Transform.position, Actor.Bones[i].Transform.rotation, 2f*GetRadius(i), UltiDraw.Black.Transparent(0.05f));
			UltiDraw.DrawArrow(Positions[i], Positions[i] + Gradients[i], 0.8f, 0.005f, 0.015f, UltiDraw.Cyan.Transparent(0.5f));
		}
		UltiDraw.End();
	}

}

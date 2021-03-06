﻿//#define DRAW_SCAN_AXES

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Object = UnityEngine.Object;
using UnityEngine;
using System.Diagnostics;
using Cybex;
using Cybex.DERAILDigital;
using HarmonyLib;
using System.Linq.Expressions;

namespace DvMod.HeadsUpDisplay
{
#if (DEBUG && DRAW_SCAN_AXES)
	public class AxesSingleton : MonoBehaviour
	{
		public static AxesSingleton? Instance0 { get; private set; }
		public static AxesSingleton? Instance1 { get; private set; }

		public void Start()
		{
			if (Instance0 == null) { Instance0 = this; this.gameObject.name = "DebugAxes0"; }
			else if (Instance1 == null) { Instance1 = this; this.gameObject.name = "DebugAxes1"; }
			else { Destroy(this.gameObject); return; }

			GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
			go.name = "Center";
			go.transform.SetParent(transform);
			go.transform.localPosition = Vector3.zero;
			go.transform.localRotation = Quaternion.identity;
			go.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
			go.GetComponent<Collider>().enabled = false;
			go.GetComponent<MeshRenderer>().sharedMaterial = new Material(go.GetComponent<MeshRenderer>().sharedMaterial) { color = Color.gray };

			go = GameObject.CreatePrimitive(PrimitiveType.Cube);
			go.name = "xAxis";
			go.transform.SetParent(transform);
			go.transform.localPosition = Vector3.zero + Vector3.right * 0.5f;
			go.transform.localRotation = Quaternion.identity;
			go.transform.localScale = new Vector3(1f, 0.05f, 0.05f);
			go.GetComponent<Collider>().enabled = false;
			go.GetComponent<MeshRenderer>().sharedMaterial = new Material(go.GetComponent<MeshRenderer>().sharedMaterial) { color = Color.red };

			go = GameObject.CreatePrimitive(PrimitiveType.Cube);
			go.name = "yAxis";
			go.transform.SetParent(transform);
			go.transform.localPosition = Vector3.zero + Vector3.up * 0.5f;
			go.transform.localRotation = Quaternion.identity;
			go.transform.localScale = new Vector3(0.05f, 1f, 0.05f);
			go.GetComponent<Collider>().enabled = false;
			go.GetComponent<MeshRenderer>().sharedMaterial = new Material(go.GetComponent<MeshRenderer>().sharedMaterial) { color = Color.green };

			go = GameObject.CreatePrimitive(PrimitiveType.Cube);
			go.name = "zAxis";
			go.transform.SetParent(transform);
			go.transform.localPosition = Vector3.zero + Vector3.forward * 0.5f;
			go.transform.localRotation = Quaternion.identity;
			go.transform.localScale = new Vector3(0.05f, 0.05f, 1f);
			go.GetComponent<Collider>().enabled = false;
			go.GetComponent<MeshRenderer>().sharedMaterial = new Material(go.GetComponent<MeshRenderer>().sharedMaterial) { color = Color.blue };
		}
	}
	public class ScanIndicator : MonoBehaviour
	{
		public static ScanIndicator? Instance0 { get; private set; }
		public static ScanIndicator? Instance1 { get; private set; }

		public void Start()
		{
			if (Instance0 == null) { Instance0 = this; this.gameObject.name = "ScanIndicator0"; }
			else if (Instance1 == null) { Instance1 = this; this.gameObject.name = "ScanIndicator1"; }
			else { Destroy(this.gameObject); return; }

			GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
			go.name = "Center";
			go.transform.SetParent(transform);
			go.transform.localPosition = Vector3.zero;
			go.transform.localRotation = Quaternion.identity;
			go.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
			go.GetComponent<Collider>().enabled = false;
			go.GetComponent<MeshRenderer>().sharedMaterial = new Material(go.GetComponent<MeshRenderer>().sharedMaterial) { color = Color.gray };

			go = GameObject.CreatePrimitive(PrimitiveType.Cube);
			go.name = "Axis";
			go.transform.SetParent(transform);
			go.transform.localPosition = Vector3.zero;
			go.transform.localRotation = Quaternion.identity;
			go.transform.localScale = new Vector3(3f, 0.05f, 0.05f);
			go.GetComponent<Collider>().enabled = false;

			go = GameObject.CreatePrimitive(PrimitiveType.Cube);
			go.name = "Direction";
			go.transform.SetParent(transform);
			go.transform.localPosition = Vector3.zero + Vector3.forward * 0.5f;
			go.transform.localRotation = Quaternion.identity;
			go.transform.localScale = new Vector3(0.05f, 0.05f, 1f);
			go.GetComponent<Collider>().enabled = false;
			go.GetComponent<MeshRenderer>().sharedMaterial = new Material(go.GetComponent<MeshRenderer>().sharedMaterial) { color = Color.yellow };
		}
	}
#endif

	public class SortedTrainSet
	{
		public List<(TrainCar trainCar, bool forward)>? cars;

		public (TrainCar trainCar, bool forward) this [TrainCar traincar] => cars.Single(t => t.trainCar == traincar);

		public SortedTrainSet (TrainCar master)
		{
			cars = new List<(TrainCar trainCar, bool forward)>() { (master, true) };

			var current = cars[0];
			Coupler? ccp = current.trainCar.frontCoupler;
			// scan for cars in front of master
			while (ccp != null)
			{
				if (!ccp.IsCoupled()) ccp = null;
				else
				{
					Coupler coupledTo = ccp.coupledTo;

					// if ccp is front && ct is front = dir is backwards to current (other)
					// if ccp is front && ct is back = dir is forwards to current (same)
					// if ccp is back && ct is front = dir is forwards to current (same)
					// id ccp is back && ct is back = dir is backwards to current (other)
					bool forward = ((ccp.isFrontCoupler && !coupledTo.isFrontCoupler) || (!ccp.isFrontCoupler && coupledTo.isFrontCoupler)) ? current.forward : !current.forward;

					TrainCar car = coupledTo.train;

					cars.Insert(0, (car, forward));

					current = (car, forward);
					// front coupler id [0] rear is [1]
					ccp = (coupledTo.isFrontCoupler) ? car.rearCoupler : car.frontCoupler;
				}
			}

			current = cars.Last();
			ccp = current.trainCar.rearCoupler;
			// scan for cars in back of master
			while (ccp != null)
			{
				if (!ccp.IsCoupled()) ccp = null;
				else
				{
					Coupler coupledTo = ccp.coupledTo;
					bool forward = ((ccp.isFrontCoupler && !coupledTo.isFrontCoupler) || (!ccp.isFrontCoupler && coupledTo.isFrontCoupler)) ? current.forward : !current.forward;
					TrainCar car = coupledTo.train;
					cars.Add((car, forward));
					current = (car, forward);
					ccp = (coupledTo.isFrontCoupler) ? car.rearCoupler : car.frontCoupler;
				}
			}
		}
	}

	public class DERAILDigitalPusher : MonoBehaviour
	{
		public static ITabletComputer? Instance;

		private static Action[] pushActions = new Action[0];
		private static int pushIndex = 0;

		private static int scanDir = 1;
		private static TrainCar? scanCar;
		private static Bogie? scanBogie;

		private static SortedTrainSet? sortedTrainSet;
		private static float trainLength = 0;

		private void Start()
		{
			Init();
			StartCoroutine(PushTrigger());
		}

		static IEnumerator PushTrigger()
		{
			ITabletComputer? tablet = Instance;
			while (true)
			{
				while (tablet == null)
				{
					UnityEngine.Debug.Log("[HEADS UP DISPLAY] > [DERAIL Digital] Tablet is null. PushTrigger skip!");
					tablet = Instance;
					yield return new WaitForSeconds(1f);
				}
				Push();

				CreateSortedTrainSet();

				yield return null;
			}
		}

		public static void Init()
		{
			IEnumerable<ITabletComputer>? t = FindObjectsOfType<MonoBehaviour>().OfType<ITabletComputer>();
			if (t == null || t.Count() == 0)
			{
				TabletLoader.ControllerInstanceCreated += Init;
				UnityEngine.Debug.LogWarning("[HEADS UP DISPLAY] > [DERAIL Digital] No ITabletComputer Instance found!");
				return;
			}
			Instance = t.ElementAt(0);
			pushActions = new Action[] { DrawUpcomingEvents, DrawConsistSpeedLimit };

			UnityEngine.Debug.LogWarning("[HEADS UP DISPLAY] > [DERAIL Digital] ITabletComputer Instance set. PushTrigger started!");
		}

		static float gradeSkipper = 0;
		static float slipSkipper = 0;
		public static void Push()
		{
			if (Instance == null) return;
			if (Instance.MasterLoco == null) return;

			var scanLoco = Instance.MasterLoco;
			var locoController = scanLoco.GetComponent<LocoControllerBase>();
			scanDir = (locoController.reverser > 0) ? 1 : (locoController.reverser < 0) ? -1 : scanDir;




#if (DEBUG && DRAW_SCAN_AXES)
			if (sortedTrainSet != null)
			{
				var temp_scanDir = (locoController.reverser > 0) ? 1 : (locoController.reverser < 0) ? -1 : scanDir;
				var temp_scanCar = temp_scanDir > 0 ? sortedTrainSet.cars.First().trainCar : sortedTrainSet.cars.Last().trainCar;
				var temp_scanBogie = temp_scanCar.Bogies[(sortedTrainSet[temp_scanCar].forward == (temp_scanDir > 0)) ? temp_scanCar.Bogies.Length - 1 : 0];

				// Instance0 for ScanCar
				if (AxesSingleton.Instance0 != null)
				{
					AxesSingleton.Instance0.transform.position = temp_scanCar.transform.position + Vector3.up * 2;
					AxesSingleton.Instance0.transform.rotation = temp_scanCar.transform.rotation;
				}
				else { new GameObject().AddComponent<AxesSingleton>(); }

				// Instance1 for ScanBogie
				if (AxesSingleton.Instance1 != null)
				{
					AxesSingleton.Instance1.transform.position = temp_scanBogie.transform.position + Vector3.up * 2;
					AxesSingleton.Instance1.transform.rotation = temp_scanBogie.transform.rotation;
				}
				else { new GameObject().AddComponent<AxesSingleton>(); }

				// Instance0 for front
				if (ScanIndicator.Instance0 != null)
				{
					ScanIndicator.Instance0.transform.position = temp_scanBogie.transform.position + Vector3.up * 1.9f;
					ScanIndicator.Instance0.transform.rotation = temp_scanBogie.transform.rotation;
					if (sortedTrainSet[temp_scanCar].forward != (temp_scanDir > 0)) ScanIndicator.Instance0.transform.Rotate(Vector3.up, 180, Space.Self);
				}
				else { new GameObject().AddComponent<ScanIndicator>(); }

				temp_scanCar = temp_scanDir > 0 ? sortedTrainSet.cars.Last().trainCar : sortedTrainSet.cars.First().trainCar;
				temp_scanBogie = temp_scanCar.Bogies[(sortedTrainSet[temp_scanCar].forward == (temp_scanDir > 0)) ? 0 : temp_scanCar.Bogies.Length - 1];

				// Instance1 for back
				if (ScanIndicator.Instance1 != null)
				{

					ScanIndicator.Instance1.transform.position = temp_scanBogie.transform.position + Vector3.up * 1.9f;
					ScanIndicator.Instance1.transform.rotation = temp_scanBogie.transform.rotation;
					if (sortedTrainSet[temp_scanCar].forward != (temp_scanDir > 0)) ScanIndicator.Instance1.transform.Rotate(Vector3.up, 180, Space.Self);
				}
				else { new GameObject().AddComponent<ScanIndicator>(); }
			}
#endif




			foreach (var group in Registry.GetProviders(scanLoco.carType).Where(g => g.Count > 0))
			{
				foreach (var dp in group)
				{
					switch (dp.Label)
					{
						case "Speed":
							Instance.SetCurrentSpeed(float.Parse(dp.GetValue(scanLoco).Split(' ')[0]));
							break;
						case "Grade":
							if (gradeSkipper > 0) gradeSkipper -= Time.deltaTime;
							else
							{
								Instance.SetCurrentGrade(float.Parse(dp.GetValue(scanLoco).Split(' ')[0].TrimStart(new char[] { '\u002b', '\u2212' })) * ((locoController.reverser >= 0) ? -1 : 1) * 10);
								gradeSkipper = 1;
							}
							break;
						case "Brake pipe":
							break;
						case "Consist mass":
							Instance.SetTrainWeight(float.Parse(dp.GetValue(scanLoco).Split(' ')[0]));
							break;
						case "Consist length":
							trainLength = float.Parse(dp.GetValue(scanLoco).Split(' ')[0]);
							Instance.SetTrainLength(trainLength);
							break;
						case "Tractive effort":
							break;
						case "Adhesion":
							break;
						case "Slip":
							if (slipSkipper > 0) slipSkipper -= Time.deltaTime;
							else
							{
								Instance.SetWheelSlip(float.Parse(dp.GetValue(scanLoco).Split(' ')[0]));
								slipSkipper = 0.25f;
							}
							break;
						default:
							break;
					}
				}
			}

			if (pushActions.Length > 0)
			{
				pushActions[pushIndex].Invoke();
				pushIndex++;
				LoopIndex(ref pushIndex, pushActions.Length);
			}
		}

		static float currentSpeedLimitNext;
		private static void DrawUpcomingEvents ()
		{
			if (Instance == null) return;
			if (sortedTrainSet == null) return;

			scanCar = scanDir > 0 ? sortedTrainSet.cars.First().trainCar : sortedTrainSet.cars.Last().trainCar;
			scanBogie = scanCar.Bogies[(sortedTrainSet[scanCar].forward == (scanDir > 0)) ? scanCar.Bogies.Length - 1 : 0];

			var track = scanBogie.track;
			if (track == null) return;

			var startSpan = scanBogie.traveller.Span;
			var direction = !(sortedTrainSet[scanCar].forward == (scanDir > 0)) ^ (scanBogie.trackDirection > 0); // !(scanDir >= 0f) ^ (scanBogie.trackDirection > 0);
			var currentGrade = TrackIndexer.Grade(scanBogie.point1) * (direction ? 1 : -1);
			var scanDistance = Instance.TrackPlannerDisplayDistance + 100;

			var events = TrackFollower.FollowTrack(
				track,
				startSpan,
				direction ? scanDistance : -scanDistance);

			var eventDescriptions = events
				.ExceptUnnamedTracks()
				.ResolveJunctionSpeedLimits()
				.FilterRedundantSpeedLimits()
				.FilterGradeEvents(currentGrade)
				.Take(Main.settings.maxEventCount)
				.TakeWhile(ev => ev.span < Main.settings.maxEventSpan);

			IEnumerable<(float span, float limit)> trackSpeedEvents = eventDescriptions.Where(e => e is SpeedLimitEvent).Cast<SpeedLimitEvent>().Select(s => ((float)s.span, (float)s.limit));
			var nextSpeedEvents = trackSpeedEvents.Where(e => e.span < 250);



			(float span, float limit) nextForSpan = trackSpeedEvents.Aggregate((a, b) => a.span < b.span ? a : b);
			(float span, float limit) nextForLimit = trackSpeedEvents.Aggregate((a, b) => a.limit < b.limit ? a : b);

			float lerpVal = Mathf.Clamp01(nextForLimit.span * 0.002f);
			float maxSpeed = Mathf.Min(Mathf.Lerp(nextForLimit.limit, speedLimitConsist, lerpVal), speedLimitConsist);
			Instance.SetSpeedMax(maxSpeed);

			if (nextForSpan.span <= 0.1f) Instance.SetSpeedLimitNext(currentSpeedLimitNext = nextForSpan.limit);
			if (currentSpeedLimitNext > nextForLimit.limit) Instance.SetSpeedLimitNext(currentSpeedLimitNext = nextForLimit.limit);



			Instance.SetTrackSpeedItems(trackSpeedEvents.ToArray());

			Instance.SetTrackGradeItems(eventDescriptions.Where(e => e is GradeEvent).Cast<GradeEvent>().Select(s => ((float)s.span, (float)s.grade * 10, -1f)).ToArray());

			var junctionEvents = eventDescriptions.Where(e => e is JunctionEvent);
			Junction? nextJunction = null;
			if (junctionEvents.Any())
			{
				nextJunction = ((JunctionEvent)junctionEvents.Aggregate((a, b) => a.span < b.span ? a : b)).junction;
				Instance.SetTrackJunctionItems(junctionEvents.Cast<JunctionEvent>().Select(s => ((float)(s.span - 1.3), (float)0)).ToArray());
			}
			Instance.NextJunction = nextJunction;
			Instance.SetNextWyeDir((nextJunction == null) ? WyeDirection.NA : (nextJunction.selectedBranch == 0) ? WyeDirection.Left : WyeDirection.Right);
		}

		static float speedLimitConsist;
		private static void DrawConsistSpeedLimit()
		{
			if (Instance == null) return;
			if (sortedTrainSet == null) return;

			scanCar = scanDir > 0 ? sortedTrainSet.cars.Last().trainCar : sortedTrainSet.cars.First().trainCar;
			scanBogie = scanCar.Bogies[(sortedTrainSet[scanCar].forward == (scanDir > 0)) ? 0 : scanCar.Bogies.Length - 1];

			var track = scanBogie.track;
			if (track == null) return;

			var startSpan = scanBogie.traveller.Span;
			var direction = !(sortedTrainSet[scanCar].forward == (scanDir > 0)) ^ (scanBogie.trackDirection > 0);
			var currentGrade = TrackIndexer.Grade(scanBogie.point1) * (direction ? 1 : -1);

			var events = TrackFollower.FollowTrack(
				track,
				startSpan,
				direction ? trainLength : -trainLength);

			var eventDescriptions = events
				.ExceptUnnamedTracks()
				.ResolveJunctionSpeedLimits()
				.FilterRedundantSpeedLimits()
				.FilterGradeEvents(currentGrade)
				.Take(Main.settings.maxEventCount)
				.TakeWhile(ev => ev.span < trainLength);

			var speeds = eventDescriptions.Where(e => e is SpeedLimitEvent).Cast<SpeedLimitEvent>().Select(s => ((float)s.span, (float)s.limit));
			var limit = (speeds.Count() > 0) ? speeds.Min(s => s.Item2) : 200;
			limit = Mathf.Min(limit, TrackFollower.GetSpeedLimit(track, startSpan, direction) ?? 0f);
			Instance.SetSpeedLimitConsist(speedLimitConsist = limit);
		}

		/// <summary>Fixes unorderly trainset</summary>
		private static void CreateSortedTrainSet ()
		{
			if (Instance != null && Instance.MasterLoco != null) sortedTrainSet = new SortedTrainSet(Instance.MasterLoco);
		}

		private static void LoopIndex(ref int num, int max)
		{
			num = (num + max) % max;
		}

#if DEBUG
		public static IEnumerator RunTest()
		{
			Stopwatch sw = new Stopwatch();

			List<Junction> junctions;
			RaycastHit[] hits = new RaycastHit[0];

			float footTot = 0, footAvg = -1, footMin = float.MaxValue, footMax = 0, footJ = 0;
			float scTot = 0, scAvg = -1, scMin = float.MaxValue, scMax = 0, scJ = 0;

			float t;
			int iterations = 2;// 2000;
			int sphereRange = 20000;

			while (true)
			{
				VisualSwitch[] switches = Object.FindObjectsOfType<VisualSwitch>();
				VisualSwitch s = switches.First();
				Junction j = s.junction;
				UnityEngine.Debug.Log($"\n----- Junction Detection Diagnostics -----\n" +
					$"\nJunction Info:\nHas Collider: {(j.GetComponent<Collider>() != null)}\nLayer: {LayerMask.LayerToName(j.gameObject.layer)} ({j.gameObject.layer})\n" +
					$"\nVisualSwitch Info:\nHas Collider: {(s.GetComponent<Collider>() != null)}\nLayer: {LayerMask.LayerToName(s.gameObject.layer)} ({s.gameObject.layer})");
				UnityEngine.Debug.Log($"\nStarting Test: ({iterations / 2} iterations each)");

				for (int i = 0; i < iterations; i++)
				{
					yield return null;

					if (i % 2 == 0)
					{
						sw.Restart();

						junctions = Object.FindObjectsOfType<Junction>().ToList();

						sw.Stop();
						t = sw.ElapsedMilliseconds;

						footJ = junctions.Count();
						footTot += t;
						if (t > footMax) footMax = t;
						if (t < footMin) footMin = t;
						if (footAvg == -1) footAvg = t;
						else footAvg = (footAvg + t) * 0.5f;
					}
					else
					{
						sw.Restart();

						hits = Physics.SphereCastAll(PlayerManager.LastLoco.transform.position, sphereRange, Vector3.up, 1, 1 << 15);

						junctions = hits
							.Where(x => x.collider.GetComponent<VisualSwitch>() != null)
							.Select(j => j.collider.GetComponent<VisualSwitch>().junction).ToList();

						sw.Stop();
						t = sw.ElapsedMilliseconds;

						scJ = junctions.Count();
						scTot += t;
						if (t > scMax) scMax = t;
						if (t < scMin) scMin = t;
						if (scAvg == -1) scAvg = t;
						else scAvg = (scAvg + t) * 0.5f;
					}
				}

				UnityEngine.Debug.Log($"\nDiagnostics Result:\n");

				UnityEngine.Debug.Log($"FindObjectsOfType:\n> Detected Junctions: {footJ}\n> t avg: {footAvg} ms\n> t min: {footMin} ms\n> t max: {footMax} ms\n> t tot: {footTot} ms");
				UnityEngine.Debug.Log($"\nSphereCastAll ({sphereRange} units range):\n> Detected Colliders: {hits.Count()}\n> Detected Junctions: {scJ}\n> t avg: {scAvg} ms\n> t min: {scMin} ms\n> t max: {scMax} ms\n> t tot: {scTot} ms");

				UnityEngine.Debug.Log($"\n------------------------------------------\n");

				yield break;
			}
		}

		private void OnGUI()
		{
			Color defaultColor = GUI.color;
			if (Instance == null) return;

			GUILayout.BeginVertical();

			GUILayout.BeginHorizontal();

			// row spacer
			GUILayout.BeginVertical();
			GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.MaxWidth(1920));
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.EndVertical(); // row spacer end

			// row last
			GUILayout.BeginVertical();

			// stats box
			GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(200));

			if (Instance.MasterLoco != null)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label($"Master [{Instance.MasterLoco.ID}]", GUILayout.Width(150));
				GUILayout.EndHorizontal();

				LocoControllerBase masterBase = Instance.MasterLoco.GetComponent<LocoControllerBase>();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Throttle", GUILayout.Width(150));
				GUILayout.Label(masterBase.throttle.ToString("n2"));
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label($"Reverser [{((masterBase.reverser > 0) ? "FWD" : (masterBase.reverser < 0) ? "BWD" : "NTR")}]", GUILayout.Width(150));
				GUILayout.Label(masterBase.reverser.ToString("n2"));
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Speed", GUILayout.Width(150));
				GUILayout.Label(masterBase.GetSpeedKmH().ToString("n2"));
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Speed (FWD)", GUILayout.Width(150));
				GUILayout.Label(masterBase.GetForwardSpeed().ToString("n2"));
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Brake", GUILayout.Width(150));
				GUILayout.Label(masterBase.brake.ToString("n2"));
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Brake Ind.", GUILayout.Width(150));
				GUILayout.Label(masterBase.independentBrake.ToString("n2"));
				GUILayout.EndHorizontal();
			}

			GUILayout.EndVertical(); // stats box end

			GUILayout.EndVertical(); // row last end

			GUILayout.EndHorizontal();


			// column spacer
			GUILayout.BeginHorizontal();
			GUILayout.BeginVertical(GUILayout.ExpandHeight(true), GUILayout.MaxHeight(1080));
			GUILayout.FlexibleSpace();
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();// column spacer end

			GUILayout.BeginHorizontal();
			GUILayout.Label("DV TS", GUILayout.Width(30));

			if (Instance.MasterLoco != null && Instance.MasterLoco.trainset.cars != null)
			{
				for (int i = 0; i < Instance.MasterLoco.trainset.cars.Count; i++)
				{
					int index = Instance.MasterLoco.trainset.cars.Count - 1 - i;
					GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(100));

					if (Instance.MasterLoco.trainset.cars[index] == Instance.MasterLoco) GUI.color = Color.yellow;
					else
					{
						if (Instance.MasterLoco.trainset.cars[index].IsLoco) { }
						else if (Instance.MasterLoco.trainset.cars[index] == Instance.MasterLoco.trainset.firstCar) GUI.color = Color.green;
						else if (Instance.MasterLoco.trainset.cars[index] == Instance.MasterLoco.trainset.lastCar) GUI.color = Color.red;
						else GUI.color = Color.gray;
					}

					GUILayout.Label(Instance.MasterLoco.trainset.cars[index].ID);

					GUI.color = defaultColor;

					GUILayout.EndVertical();
				}
			}

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("CBX TS", GUILayout.Width(30));

			if (sortedTrainSet != null && sortedTrainSet.cars != null)
			{
				for (int i = 0; i < sortedTrainSet.cars.Count; i++)
				{
					int index = sortedTrainSet.cars.Count - 1 - i;
					GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(100));

					if (sortedTrainSet.cars[index].trainCar == Instance.MasterLoco) GUI.color = Color.yellow;
					else
					{
						if (sortedTrainSet.cars[index].trainCar.IsLoco) { }
						else if (sortedTrainSet.cars[index].trainCar == sortedTrainSet.cars.First().trainCar) GUI.color = Color.green;
						else if (sortedTrainSet.cars[index].trainCar == sortedTrainSet.cars.Last().trainCar) GUI.color = Color.red;
						else GUI.color = Color.gray;
					}

					GUILayout.Label($"{sortedTrainSet.cars[index].trainCar.ID} {((sortedTrainSet.cars[index].trainCar == Instance.MasterLoco) ? "[M]":"")}");
					GUILayout.Label((sortedTrainSet.cars[index].forward) ? "FWD" : "BWD");

					GUI.color = defaultColor;

					GUILayout.EndVertical();
				}
			}

			GUILayout.EndHorizontal();

			GUILayout.EndVertical();
		}
#endif
	}
}

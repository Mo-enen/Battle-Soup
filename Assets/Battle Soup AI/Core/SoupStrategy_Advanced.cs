using System.Collections;
using System.Collections.Generic;


namespace BattleSoupAI {
	public abstract class SoupStrategy_Advanced : SoupStrategy {


		// Data
		protected int CoracleCooldown = 0;
		protected int WhaleCooldown = 0;
		protected int SquidCooldown = 0;
		protected int TurtleCooldown = 0;
		protected int OwnAliveShipCount = 0;
		protected int OpponentAliveShipCount = 0;
		protected int ExposedShipCount = 0;
		protected int FoundShipCount = 0;
		protected int ShipWithMinimalPotentialPos = -1;
		protected int TileCount_RevealedWater = 0;
		protected int TileCount_RevealedShip = 0;
		protected int TileCount_HittedShip = 0;
		protected ShipPosition?[] ShipFoundPosition = null;
		protected List<ShipPosition>[] HiddenPotentialPos = null;
		protected List<ShipPosition>[] ExposedPotentialPos = null;
		protected float[,,] HiddenValues = new float[0, 0, 0];
		protected float[,,] ExposedValues = new float[0, 0, 0];
		protected int[] MostExposed = null;
		protected (Int2 pos, float max)[] HiddenValueMax = null;
		protected (Int2 pos, float max)[] ExposedValueMax = null;


		// API
		public override AnalyseResult Analyse (BattleInfo ownInfo, BattleInfo oppInfo, int usingAbilityIndex = -1) {

			// Check
			string msg = AvailableCheck(ownInfo, oppInfo);
			if (!string.IsNullOrEmpty(msg)) {
				return new AnalyseResult() { ErrorMessage = msg, };
			}

			// Cooldown
			CoracleCooldown = ownInfo.ShipsAlive[0] ? ownInfo.Cooldowns[0] : -1;
			WhaleCooldown = ownInfo.ShipsAlive[1] ? ownInfo.Cooldowns[1] : -1;
			SquidCooldown = ownInfo.ShipsAlive[2] ? ownInfo.Cooldowns[2] : -1;
			TurtleCooldown = ownInfo.ShipsAlive[3] ? ownInfo.Cooldowns[3] : -1;

			// Alive Ship Count
			OwnAliveShipCount = ownInfo.AliveShipCount;
			OpponentAliveShipCount = oppInfo.AliveShipCount;

			// Revealed Tile Count
			TileCount_RevealedShip = 0;
			TileCount_HittedShip = 0;
			TileCount_RevealedWater = 0;
			for (int y = 0; y < oppInfo.MapSize; y++) {
				for (int x = 0; x < oppInfo.MapSize; x++) {
					switch (oppInfo.Tiles[x, y]) {
						case Tile.RevealedShip:
							TileCount_RevealedShip++;
							break;
						case Tile.HittedShip:
							TileCount_HittedShip++;
							break;
						case Tile.RevealedWater:
							TileCount_RevealedWater++;
							break;
					}
				}
			}

			// Potential
			if (!CalculatePotentialPositions(
				oppInfo,
				Tile.GeneralWater,
				Tile.GeneralWater,
				ref HiddenPotentialPos
			)) {
				return new AnalyseResult() {
					ErrorMessage = "Fail to calculate hidden positions",
				};
			}
			if (!CalculatePotentialPositions(
				oppInfo,
				Tile.HittedShip | Tile.RevealedShip,
				Tile.GeneralWater | Tile.HittedShip | Tile.RevealedShip,
				ref ExposedPotentialPos
			)) {
				return new AnalyseResult() {
					ErrorMessage = "Fail to calculate exposed positions",
				};
			}
			RemoveImpossiblePositions(
				oppInfo, ref HiddenPotentialPos, ref ExposedPotentialPos
			);

			// Values
			if (!CalculatePotentialValues(
				oppInfo,
				HiddenPotentialPos,
				ref HiddenValues, out _, out _
			)) {
				return new AnalyseResult() {
					ErrorMessage = "Fail to calculate hidden values",
				};
			}

			if (!CalculatePotentialValues(
				oppInfo,
				ExposedPotentialPos,
				ref ExposedValues, out _, out _
			)) {
				return new AnalyseResult() {
					ErrorMessage = "Fail to calculate exposed values",
				};
			}

			// Get Most Exposed
			if (MostExposed == null || MostExposed.Length != oppInfo.Ships.Length) {
				MostExposed = new int[oppInfo.Ships.Length];
			}
			for (int i = 0; i < oppInfo.Ships.Length; i++) {
				MostExposed[i] = GetMostExposedPositionIndex(oppInfo.Ships[i], oppInfo.Tiles, ExposedPotentialPos[i]);
			}

			// Get Value Max
			if (HiddenValueMax == null || HiddenValueMax.Length != oppInfo.Ships.Length + 1) {
				HiddenValueMax = new (Int2, float)[oppInfo.Ships.Length + 1];
			}
			if (ExposedValueMax == null || ExposedValueMax.Length != oppInfo.Ships.Length + 1) {
				ExposedValueMax = new (Int2, float)[oppInfo.Ships.Length + 1];
			}
			for (int i = 0; i < oppInfo.Ships.Length + 1; i++) {
				HiddenValueMax[i] = GetMaxValue(HiddenValues, i);
				ExposedValueMax[i] = GetMaxValue(ExposedValues, i);
			}

			// Exposed Ship Count
			ExposedShipCount = 0;
			for (int i = 0; i < ExposedPotentialPos.Length; i++) {
				int count = ExposedPotentialPos[i].Count;
				if (count > 0) {
					ExposedShipCount++;
				}
			}

			// Ship Found
			FoundShipCount = 0;
			if (ShipFoundPosition == null || ShipFoundPosition.Length != oppInfo.Ships.Length) {
				ShipFoundPosition = new ShipPosition?[oppInfo.Ships.Length];
			}
			for (int i = 0; i < ShipFoundPosition.Length; i++) {
				if (oppInfo.KnownPositions[i].HasValue) {
					ShipFoundPosition[i] = oppInfo.KnownPositions[i].Value;
					FoundShipCount++;
				} else if (HiddenPotentialPos[i].Count + ExposedPotentialPos[i].Count == 1) {
					ShipFoundPosition[i] = HiddenPotentialPos[i].Count > 0 ? HiddenPotentialPos[i][0] : ExposedPotentialPos[i][0];
					FoundShipCount++;
				}
			}

			// Ship with Minimal Potential-Pos-Count
			ShipWithMinimalPotentialPos = GetShipWithMinimalPotentialPosCount(oppInfo, HiddenPotentialPos, ExposedPotentialPos);

			return new AnalyseResult() { ErrorMessage = "", };
		}


		protected virtual string AvailableCheck (BattleInfo ownInfo, BattleInfo oppInfo) {

			if (oppInfo.Ships == null || oppInfo.Ships.Length == 0) {
				return "Can't analyse when opponent don't have ship";
			}
			if (FleetID.Length == 0 || ownInfo.Ships.Length == 0) {
				return "Can't analyse when the bot don't have ship";
			}
			if (ownInfo.Ships.Length != FleetID.Length) {
				string msg = $"There must be {FleetID.Length} ships (";
				foreach (var id in FleetID) {
					msg += id + ", ";
				}
				msg += ")";
				return msg;
			}
			for (int i = 0; i < FleetID.Length; i++) {
				var ship = ownInfo.Ships[i];
				if (ship.GlobalID != FleetID[i]) {
					return $"Ship No.{i + 1} must be {FleetID[i]}, not {ship.GlobalID}";
				}
			}
			if (ownInfo.AliveShipCount == 0 || oppInfo.AliveShipCount == 0) {
				return "No own/opponent ship alive now";
			}

			return "";
		}


		// Util
		public bool TryAttackShip (BattleInfo info, int targetIndex, Tile filter, out Int2 pos) {
			pos = default;
			var body = info.Ships[targetIndex].Body;
			foreach (var sPos in ExposedPotentialPos[targetIndex]) {
				foreach (var v in body) {
					var _pos = sPos.GetPosition(v);
					if (filter.HasFlag(info.Tiles[_pos.x, _pos.y])) {
						pos = _pos;
						return true;
					}
				}
			}
			foreach (var sPos in HiddenPotentialPos[targetIndex]) {
				foreach (var v in body) {
					var _pos = sPos.GetPosition(v);
					if (filter.HasFlag(info.Tiles[_pos.x, _pos.y])) {
						pos = _pos;
						return true;
					}
				}
			}
			return false;
		}


	}
}

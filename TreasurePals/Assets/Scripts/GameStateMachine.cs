﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;


namespace Tabletop 
{
	public enum PlayerColors {
		Red, Green, Blue, Yellow, Purple, Orange
	}

	public enum PlayerStates
	{
		LeavingShip, Diving, Ascending, ReturnedToShip, LostAtSea
	}

	//A-D tier 1,2,3,4 
	//E is empty treasure
	//F is combo treasure
	public enum TreasureType {
		A, B, C, D, E, F
	}

	public enum TreasureStates
	{
		Neutral, Collected, Captured;
	}

	public enum GameStates {
		GameStarted,GameInProgress,GameEnded,
		TreasureScoring,TreasureTotaled,VictorDecided
	} 

	public enum RoundStates {
		RoundStarted,RoundInProgress,RoundEnded,
		AirDeducted,AirEmpty,
		TreasureScoring
	}

	public enum TurnStates {
		TurnStarted,TurnEnded,
		PlayerRolling,PlayerRolled,PlayerMoving,PlayerMoved,
		TreasureAvailable,TreasureUnavailable,
		TreasureCollected,TreasurePassed
	}



	public class TreasureLocation
	{

		public Player player = null;
		public Treasure treasure = null;

		public TreasureLocation()
		{

		}
	}

	public class Player {

		public int currentPosition = 0;
		public PlayerColors color;
		//public bool isDiving = true;
		public PlayerStates state = PlayerStates.LeavingShip;

		public List<Treasure> collectedTreasures;
		public List<Treasure> capturedTreasures;

		public Player (PlayerColors color) {

			this.color = color;
			collectedTreasures = new List<Treasure>();
		}

		// the player returned safely to the ship!
		public void placeInShip()
		{
			Debug.Log("PlaceInShip: " + color);
			currentPosition = -1;
			state = PlayerStates.ReturnedToShip;

			capturedTreasures = new List<Treasure>(collectedTreasures);
			collectedTreasures.Clear();
		}

		public void returnToShip() {

			if (state != PlayerStates.Diving)
			{
				throw new System.Exception("player is not diving.");
			}

			if (state == PlayerStates.Diving)
			{
				state = PlayerStates.Ascending;
			}
		}

		public void reset()
		{
			currentPosition = 0;
			state = PlayerStates.LeavingShip;
		}
	}

	public class ScoreReport
	{

		PlayerColors color;
		Dictionary<TreasureType, int> treasureTypeTotals;
		int totalScore;

		ScoreReport(PlayerColors color)
		{
			this.color = color;
			this.totalScore = 0;

			//				TreasureType[] values = Enum.GetValues(typeof(TreasureType));
			//
			//				foreach (TreasureType value in values) {
			//					treasureTypeTotals.Add(value,0);
			//				}
		}
	}

	public class StateMachine {

		// can be received from the server
		// fixed order of treasure values
		int[][] startingTreasures;

		public List<Player> players;

		public int currentGameState;

		public int firstPlayer = 0;

		public PlayerColors[] playerOrder;

		public List<Treasure> treasureQueue = new List<Treasure> ();
		public List<Treasure> treasureCollected = new List<Treasure> ();
		public List<Treasure> treasureCaptured = new List<Treasure> ();

		public Dictionary<PlayerColors,List<Treasure>> treasureCollectedReport;
		public Dictionary<PlayerColors,List<Treasure>> treasureCapturedReport;
		public Dictionary<PlayerColors,int> treasureScoredReport;

		public List<TreasureLocation> treasureLocations = new List<TreasureLocation> ();

		public int currentRound = 0;
		public RoundStates currentRoundState = RoundStates.RoundEnded;
		public int currentAir = 0;

		static int maxRounds = 3;
		static int maxAir = 25;
		static int maxRoll = 6;
		static int minRoll = 2;

		public int numberOfPlayers = 0;

		public Player currentPlayer;
		public int currentPlayerIndex = 0;
		public int currentPlayerRoll = 0;
		public int currentPlayerMovement = 0;
		public TurnStates currentTurnState = TurnStates.TurnEnded;
		public int lastRoll = 0;

		public StateMachine() {

			players = new List<Player> ();
			treasureCollectedReport = new Dictionary<PlayerColors, List<Treasure>> ();
			treasureCapturedReport = new Dictionary<PlayerColors, List<Treasure>> ();
			treasureScoredReport = new Dictionary<PlayerColors, int>();
		}

		// GAME SETUP
		//===========

		public void setupGameForPlayers ( List<PlayerColors> selectedPlayers  ) {

			Debug.Log ("setupGameForPlayers");

			foreach (PlayerColors playerColor in selectedPlayers) {
				spawnPlayer (playerColor);

				treasureCollectedReport.Add (playerColor, new List<Treasure> ());
				treasureCapturedReport.Add (playerColor, new List<Treasure> ());
			}
				
			generateStartingTreasures ();
			setupTreasures ();
		}


		private void generateStartingTreasures () {

			// TODO randomly generate values
			// TODO receive starting treasures from server
			startingTreasures = new int[4][];

			startingTreasures [0] = new int[] { 0, 0, 3, 4, 5, 3, 2 };
			startingTreasures [1] = new int[] { 6, 7, 8, 9, 10, 9, 8, 7 };
			startingTreasures [2] = new int[] { 11, 12, 13, 14, 15, 14, 13, 12 };
			startingTreasures [3] = new int[] { 16, 17, 18, 19, 20, 19, 18, 17 };
		}

		public void setPlayerOrder (PlayerColors[] _playerOrder) {

			this.playerOrder = _playerOrder;
		}

		private void spawnPlayer (PlayerColors playerColor) {

			Debug.Log ( String.Format("spawn player {0}",playerColor) );

			Player newPlayer = new Player (playerColor);
			players.Add (newPlayer);
		}


		private void setupTreasures () {

			Debug.Log ("setup treasures");

			treasureQueue = new List<Treasure> ();

			for (var t = 0; t < startingTreasures.GetLength(0); t++) {

				int[] treasuresOfType = startingTreasures [t];

				for (var i = 0; i < treasuresOfType.Length; i++) {

					Treasure newTreasure;
					TreasureType treasureType = 0;

					switch (t) {

					case 0:
						treasureType = TreasureType.A;
						break;

					case 1:
						treasureType = TreasureType.B;
						break;

					case 2:
						treasureType = TreasureType.C;
						break;

					case 3:
						treasureType = TreasureType.D;
						break;

					}

					newTreasure = new Treasure(treasureType,treasuresOfType[i]);
					treasureQueue.Add (newTreasure);
				}

			}

			for (var t=0; t < treasureQueue.Count; t++) 
			{
				treasureLocations.Add(new TreasureLocation());
				treasureLocations[t].treasure = treasureQueue[t];
				Debug.Log(String.Format("location: {0}, treasure: {1}", treasureLocations[t].treasure.type, treasureQueue[t].type));
			}

			if (treasureQueue.Count != treasureLocations.Count) 
			{
				throw new System.Exception ("treasure queue does not match treasure locations.");
			}

			Debug.Log( String.Format("Treasure queue: {0}, locations queue: {1}", treasureQueue.Count, treasureLocations.Count) );
		}




// GAME CLEANUP
//=============

		public void proceedToScoring() {

//			// create list of each Player's collected treasures
//			// treasureScoredReport = new Dictionary<PlayerColors,int> ();
//			treasureScoredReport = new List<ScoreReport> ();
//
//			foreach (Player player in players) {
//				
//				treasureScoredReport.Add (player.color, 0);
//			}
//
//			foreach (Treasure treasure in treasureCaptured) {
//
//				PlayerColors color = treasure.collectedByPlayer;
//
//				treasureScoredReport [color] += treasure.value;
//			}

			// account for number of treasures of each type to break ties

//			foreach (KeyValuePair item in treasureScoredReport) {
//
//			}
//
			// sort players scores
			//treasureScoredReport.OrderBy( (item) => { return item. }

		}


		// Remove all collected treasures from queue
		public void cleanupTreasures()
		{

			// each player tracks his own captured treasures
			foreach (Player player in players)
			{
				foreach (Treasure treasure in player.capturedTreasures)
				{
					treasureCaptured.Add(treasure);
				}

				foreach (Treasure aCollectedTreasure in player.collectedTreasures)
				{
					treasureCollected.Add(aCollectedTreasure);
				}
			}

			foreach (Treasure treasure in treasureQueue)
			{
				if (treasure.state == TreasureStates.Collected ||
				    treasure.state == TreasureStates.Captured)
				{
					treasureQueue.Remove(treasure);
				}
			}

			List<Treasure> treasureCombos = new List<Treasure>();

			// create combo treasures from collected treasures
			foreach (Treasure treasure in treasureCollected)
			{
				//TODO treasureCombos.Add(comboTreasure);
				// treasureQueue.Add(comboTreasure);
			}

			// redistribute collected treasures into groups of 3 treasures at bottom of sea
			treasureCollected.Clear();


			// reset treasure locations
			for (var t = 0; t < treasureLocations.Count; t++)
			{

				if (treasureQueue.ElementAt(t) != null)
				{
					treasureLocations[t].treasure = treasureQueue[t];

				}
				else {
					treasureLocations[t].treasure = null;
				}

			}

		}

		public void generateTreasureReport () {

			treasureCollectedReport = new Dictionary<PlayerColors,List<Treasure>> ();
			treasureCapturedReport = new Dictionary<PlayerColors,List<Treasure>> ();

			foreach (Treasure treasure in treasureCollected) {

				PlayerColors color = treasure.owner.color;

				treasureCollectedReport [color].Add (treasure);
			}

			foreach (Treasure treasure in treasureCaptured) {

				PlayerColors color = treasure.owner.color;

				treasureCapturedReport [color].Add (treasure);
			}
		}


//	GAME LOOP
//===========

		public void reportGameState()
		{
			foreach (Player player in players)
			{
				Debug.Log(String.Format("currentPlayerIndex: {0}, currentPlayer:{1} position:{2}",
									currentPlayerIndex, currentPlayer.color, currentPlayer.currentPosition));
			}

			Debug.Log(String.Format("currentRound:{0} currentAir:{1} roundState:{2} turnState:{3}",
									currentRound, currentAir, currentRoundState, currentTurnState));
		}

		public void startNextRound() {

			Debug.Log ("starting next round");


			if (currentRoundState != RoundStates.RoundEnded) {
				throw new System.Exception ("Cannot start next round, round not over");
			}

			if (currentRound > 0)
			{
				cleanupTreasures();

				// reset players
				foreach (Player player in players)
				{
					player.reset();
				}
			}
				

			if (currentRound < maxRounds) {
				currentRound++;
				currentRoundState = RoundStates.RoundStarted;
				currentAir = maxAir;
				Debug.Log(String.Format("Round started: {0} roundState {1} maxAir {2}",
				                        currentRound,currentRoundState,currentAir));

			} else {
				Debug.Log("all rounds over");
				proceedToScoring ();
			}


		}

		public void startNextTurn() {

			Debug.Log("startNextTurn turnState:" + currentTurnState + " roundState: " + currentRoundState);

			if (currentRoundState == RoundStates.RoundEnded) {
				throw new System.Exception ("Cannot start turn, round ended");
			} 
			else if (currentTurnState != TurnStates.TurnEnded) {
				throw new System.Exception ("Cannot start turn, turn not over");
			}

			if (currentRoundState == RoundStates.RoundStarted) {

				currentPlayerIndex = firstPlayer;
				currentRoundState = RoundStates.RoundInProgress;

			} 
			else if (currentRoundState == RoundStates.RoundInProgress){

				// skip player if returned to ship
				do
				{
					currentPlayerIndex++;

					if (currentPlayerIndex >= players.Count)
						currentPlayerIndex = 0;
				}
				while (players[currentPlayerIndex].state == PlayerStates.ReturnedToShip);
			}

			currentPlayer = players[currentPlayerIndex];
			currentTurnState = TurnStates.TurnStarted;

			// deduct air
			currentAir -= currentPlayer.collectedTreasures.Count;

			// check if round over
			if (currentAir <= 0)
			{
				endRound();
			}

			reportGameState();
		}

		public void endTurn()
		{

			if (currentTurnState != TurnStates.TreasurePassed &&
				currentTurnState != TurnStates.TreasureCollected &&
				currentTurnState != TurnStates.TreasureUnavailable)
			{
				throw new System.Exception("Player has not resolved treasure collection.");
			}

			currentTurnState = TurnStates.TurnEnded;

			Debug.Log("Turn over " + currentPlayer.color);
		}

		public void endRound()
		{
			if (currentRoundState != RoundStates.RoundInProgress)
			{
				throw new System.Exception("round not in progress");
			}

			currentRoundState = RoundStates.RoundEnded;

			foreach (Player player in players)
			{
				if (player.state != PlayerStates.ReturnedToShip)
				{
					player.state = PlayerStates.LostAtSea;
				}
			}

			Debug.Log("round ended " + currentRound);
		}

// ROLLING
//========

		public void rollForCurrentPlayer() {

			Debug.Log("rollForCurrentPlayer "+currentTurnState);

			if (currentTurnState != TurnStates.TurnStarted) {
				throw new System.Exception ("Rolling only allowed at start of turn");
			}

			lastRoll = getRandomRollValue();
			currentTurnState = TurnStates.PlayerRolling;

			Debug.Log(String.Format("currentPlayerRoll: {0} roundState: {1} turnState:{2}",
			                        lastRoll, currentRoundState, currentTurnState));
		}


		T getDiceRoll<T>(List<T> diceValues)
		{
			var rand = new System.Random();
			var index = rand.Next(0, diceValues.Count);
			var item = diceValues[index];

			return item;
		}

		public int getRandomRollValue()
		{
			List<int> diceValues = new List<int>{ 1, 2, 3, 1, 2, 3 };

			int roll1 = getDiceRoll<int>(diceValues);
			int roll2 = getDiceRoll<int>(diceValues);

			return roll1 + roll2;
		}

		public void setCurrentPlayerRoll (int rollValue) {

			if (currentTurnState != TurnStates.PlayerRolling) {
				throw new System.Exception ("Player roll is not pending.");
			}

			if (rollValue < minRoll || rollValue > maxRoll) {
				throw new System.Exception ("Invalid Roll value");
			}

			currentPlayerRoll = rollValue;
			currentPlayerMovement =  Math.Max(currentPlayerRoll - currentPlayer.collectedTreasures.Count, 0);
			currentTurnState = TurnStates.PlayerRolled;

			Debug.Log(String.Format("after Roll, currentPlayerRoll: {0} playerMovement: {1} turnState:{2}",
			                        lastRoll, currentPlayerMovement, currentTurnState));
		}

// MOVEMENT
//========

		public bool isCurrentPlayerDiving()
		{

			return currentPlayer.state == PlayerStates.Diving;
		}

		public bool directCurrentPlayerToShip()
		{

			if (currentPlayer.state == PlayerStates.Diving)
			{
				currentPlayer.returnToShip();

				return true;
			}
			else {
				return false;
			}
		}

		public void commitMovement() {

			if (currentTurnState != TurnStates.PlayerRolled) {
				throw new System.Exception ("Player roll has not rolled for movement.");
			}

			currentTurnState = TurnStates.PlayerMoving;

			// if player can move at all
			if (currentPlayerMovement > 0) 
			{
				applyMovementForCurrentPlayer (currentPlayerMovement);
				currentTurnState = TurnStates.PlayerMoved;

				// check if all players returned to ship
				int playersInShip = 0;
				foreach (Player player in players) 
				{
					if (player.state == PlayerStates.ReturnedToShip) playersInShip++;
				}

				if (playersInShip == players.Count)
				{
					Debug.Log("all players returned to ship");
					endRound();
					return;
				}

				if (getTreasureAtCurrentPlayerLocation ()) {
					currentTurnState = TurnStates.TreasureAvailable;
				} else {
					currentTurnState = TurnStates.TreasureUnavailable;
				}

			} else {
				currentTurnState = TurnStates.PlayerMoved;
			}

			Debug.Log(String.Format("turnState after movement: {0}", currentTurnState));
		}

		private int[] applyMovementForCurrentPlayer (int distanceToMove) {

			Debug.Log("applyMovementForCurrentPlayer "+ currentPlayer.color +
			          " currentPosition: "+currentPlayer.currentPosition+
			          " distance:" + distanceToMove);

			// return array of movements
			int[] movementHistory = new int[distanceToMove];
			int historyIndex = 0;

			movementHistory [historyIndex] = currentPlayer.currentPosition;

			while (distanceToMove > 0) {

				Debug.Log("distance to move: " + distanceToMove);
				// find next empty location
				bool locatedNextEmptyLocation = false;

				while (locatedNextEmptyLocation == false) {

					if (currentPlayer.state == PlayerStates.Diving)
					{
						currentPlayer.currentPosition+=1;
					}
					else if (currentPlayer.state == PlayerStates.Ascending)
					{
						currentPlayer.currentPosition-=1;
					}

					Debug.Log("new position: " + currentPlayer.currentPosition);

					// player reached end of treasure locations
					if (currentPlayer.currentPosition >= treasureLocations.Count)
					{
						distanceToMove = 0;
						currentPlayer.placeInShip();
						break;
					}

					else if (currentPlayer.currentPosition <= 0)
					{
						distanceToMove = 0;
						currentPlayer.returnToShip();
						break;
					}

					Debug.Log("currentPlayer position: " + currentPlayer.currentPosition);

					TreasureLocation currentLocation = treasureLocations[currentPlayer.currentPosition];

					// skip over location occupied by player
					if (treasureLocations [currentPlayer.currentPosition].player==null) {

						// found empty location
						movementHistory [historyIndex] = currentPlayer.currentPosition;

						historyIndex++;
						locatedNextEmptyLocation = true;

						Debug.Log("found next empty location: " + currentPlayer.currentPosition);

						// counted 1 movement
						distanceToMove--;
					} 

				}

			}

			TreasureLocation locationAfterMovement = treasureLocations[currentPlayer.currentPosition];
			locationAfterMovement.player = currentPlayer;

			return movementHistory;
		}


// TREASURE SELECTION
//===================

		public bool getTreasureAtCurrentPlayerLocation () {

			TreasureLocation currentLocation = treasureLocations [currentPlayer.currentPosition];

			Debug.Log(String.Format("treasure at location {0}",
									currentPlayer.currentPosition));

			//Debug.Log(String.Format("treasure at location {0}: {1}, {2}",
			//                        currentPlayer.currentPosition,
			//                        currentLocation.treasure.type,
			//                        currentLocation.treasure.value));

			if (currentLocation.treasure == null) {
				return false;
			} else {
				return true;
			}
		}


		public void selectTreasure (bool willCollectTreasure) {

			if (getTreasureAtCurrentPlayerLocation () == false) {
				throw new System.Exception ("No treasure to collect at this location");
			}

			if (willCollectTreasure)
			{
				Debug.Log("collecting treasure");
				TreasureLocation currentLocation = treasureLocations[currentPlayer.currentPosition];

				// player collects treasure
				Treasure currentTreasure = currentLocation.treasure;
				currentTreasure.collect(currentPlayer);
				currentPlayer.collectedTreasures.Add(currentTreasure);

				treasureCollected.Add(currentTreasure);

				// clear treasure at location
				currentLocation.treasure = null;

				currentTurnState = TurnStates.TreasureCollected;
			}
			else 
			{
				Debug.Log("declining treasure");
				currentTurnState = TurnStates.TreasurePassed;
			}
		}


		public void returnTreasure(int collectedTreasureIndex)
		{
			if (currentPlayer.collectedTreasures.Count == 0)
			{
				throw new System.Exception("current player has no treasures to return");
			}
			else if (getTreasureAtCurrentPlayerLocation())
			{
				throw new System.Exception("current location has treasure.");
			}
			else if (currentPlayer.collectedTreasures.ElementAt(collectedTreasureIndex)
					 == null)
			{
				throw new System.Exception("no collected treasure at this index");
			}

			Treasure treasureToReturn = currentPlayer.collectedTreasures.ElementAt(collectedTreasureIndex);
			treasureToReturn.release();

			currentPlayer.collectedTreasures.Remove(treasureToReturn);
			treasureLocations[currentPlayer.currentPosition].treasure = treasureToReturn;
		}


	}


}

public class GameStateMachine : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}

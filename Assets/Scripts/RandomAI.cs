using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomAI : IChessAI {
    public int play_turn() {
        List<int> all_moves = ChessGame.generate_moves_auto();
        return all_moves[Random.Range(0, all_moves.Count)];
    }

    public int get_evaluated_moves() {
        return 1;
    }

    public void notify_move(int move) {

    }

    public void retake() {

    }

}

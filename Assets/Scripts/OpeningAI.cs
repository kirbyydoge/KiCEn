using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpeningAI : IChessAI {

    private IChessAI fallback;
    private List<int> moves;

    public OpeningAI(IChessAI fallback) {
        this.fallback = fallback;
    }

    public int play_turn() {
        
        return fallback.play_turn();
    }

    public void notify_move(int move) {
        moves.Add(move);
    }

    public void retake() {
        moves.Remove(moves.Count - 1);
    }

    public int get_evaluated_moves() {
        return fallback.get_evaluated_moves();
    }

}

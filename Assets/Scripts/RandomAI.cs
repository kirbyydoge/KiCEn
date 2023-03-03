using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomAI : IChessAI {
    public Move play_turn() {
        List<List<Move>> all_moves = ChessGame.generate_all_moves_auto();
        List<Move> piece_moves = null;
        do {
            int index = Random.Range(0, all_moves.Count);
            piece_moves = all_moves[index];
            all_moves.RemoveAt(index);
        } while (piece_moves == null || piece_moves.Count == 0);
        return piece_moves[Random.Range(0, piece_moves.Count)];
    }

    public int get_evaluated_moves() {
        return 1;
    }
}

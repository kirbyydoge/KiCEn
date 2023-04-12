using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IChessAI {
    public int play_turn();

    public void notify_move(int move);

    public int get_evaluated_moves();
}

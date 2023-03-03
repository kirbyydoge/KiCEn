using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IChessAI {
    public Move play_turn();
    public int get_evaluated_moves();
}

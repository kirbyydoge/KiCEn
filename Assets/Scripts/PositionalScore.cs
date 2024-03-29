using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionalScore
{
    public static int[] piece_score = new int[] {
        100,
        300,
        350,
        500,
        900,
        9999
    };

    public static int[,] captures_lookup = new int[,] {
        {105, 205, 305, 405, 505, 605,  105, 205, 305, 405, 505, 605},
        {104, 204, 304, 404, 504, 604,  104, 204, 304, 404, 504, 604},
        {103, 203, 303, 403, 503, 603,  103, 203, 303, 403, 503, 603},
        {102, 202, 302, 402, 502, 602,  102, 202, 302, 402, 502, 602},
        {101, 201, 301, 401, 501, 601,  101, 201, 301, 401, 501, 601},
        {100, 200, 300, 400, 500, 600,  100, 200, 300, 400, 500, 600},
        {105, 205, 305, 405, 505, 605,  105, 205, 305, 405, 505, 605},
        {104, 204, 304, 404, 504, 604,  104, 204, 304, 404, 504, 604},
        {103, 203, 303, 403, 503, 603,  103, 203, 303, 403, 503, 603},
        {102, 202, 302, 402, 502, 602,  102, 202, 302, 402, 502, 602},
        {101, 201, 301, 401, 501, 601,  101, 201, 301, 401, 501, 601},
        {100, 200, 300, 400, 500, 600,  100, 200, 300, 400, 500, 600}
    };

    public static int[,] positional_score = new int[5, 64]{
        // Pawn
        {
            90,  90,  90,  90,  90,  90,  90,  90,
            30,  30,  30,  40,  40,  30,  30,  30,
            20,  20,  20,  30,  30,  30,  20,  20,
            10,  10,  10,  20,  20,  10,  10,  10,
            5,   5,  10,  20,  20,   5,   5,   5,
            0,   0,   0,   5,   5,   0,   0,   0,
            0,   0,   0, -10, -10,   0,   0,   0,
            0,   0,   0,   0,   0,   0,   0,   0
        },

        // Knight
        {
            -5,   0,   0,   0,   0,   0,   0,  -5,
            -5,   0,   0,  10,  10,   0,   0,  -5,
            -5,   5,  20,  20,  20,  20,   5,  -5,
            -5,  10,  20,  30,  30,  20,  10,  -5,
            -5,  10,  20,  30,  30,  20,  10,  -5,
            -5,   5,  20,  10,  10,  20,   5,  -5,
            -5,   0,   0,   0,   0,   0,   0,  -5,
            -5, -10,   0,   0,   0,   0, -10,  -5
        },

        // Bishop
        {
            0,   0,   0,   0,   0,   0,   0,   0,
            0,   0,   0,   0,   0,   0,   0,   0,
            0,   0,   0,  10,  10,   0,   0,   0,
            0,   0,  10,  20,  20,  10,   0,   0,
            0,   0,  10,  20,  20,  10,   0,   0,
            0,  10,   0,   0,   0,   0,  10,   0,
            0,  30,   0,   0,   0,   0,  30,   0,
            0,   0, -10,   0,   0, -10,   0,   0
        },

        // Rook
        {
            50,  50,  50,  50,  50,  50,  50,  50,
            50,  50,  50,  50,  50,  50,  50,  50,
            0,   0,  10,  20,  20,  10,   0,   0,
            0,   0,  10,  20,  20,  10,   0,   0,
            0,   0,  10,  20,  20,  10,   0,   0,
            0,   0,  10,  20,  20,  10,   0,   0,
            0,   0,  10,  20,  20,  10,   0,   0,
            0,   0,   0,  20,  20,   0,   0,   0
        },

        // Queen
        {
            0,   0,   0,   0,   0,   0,   0,   0,
            0,   0,   5,   5,   5,   5,   0,   0,
            0,   5,   5,  10,  10,   5,   5,   0,
            0,   5,  10,  20,  20,  10,   5,   0,
            0,   5,  10,  20,  20,  10,   5,   0,
            0,   0,   5,  10,  10,   5,   0,   0,
            0,   5,   5,  -5,  -5,   0,   5,   0,
            0,   0,   5,   0, -15,   0,  10,   0
        }
    };

}

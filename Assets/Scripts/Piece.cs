using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Piece {
    public PieceColor color;
    public PieceType type;
    public int hash;

    public Piece(PieceColor color, PieceType type) {
        this.color = color;
        this.type = type;
        hash = get_hash(color, type);
    }

    public static int get_hash(PieceColor color, PieceType type) {
        return ((int)color << 3) + ((int)type);
    }
}
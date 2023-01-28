using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PieceColor {
    BLACK, WHITE
};

public enum PieceType {
    PAWN, KNIGHT, BISHOP, ROOK, QUEEN, KING
};

public static class ChessGame {

    public static PieceColor turn;
    public static Piece[,] board;
    public static List<Piece> white_pieces;
    public static List<Piece> black_pieces;
    
    static ChessGame() {
        board = new Piece[8, 8];
        white_pieces = new List<Piece>();
        black_pieces = new List<Piece>();
        for (int i = 0; i < board.Rank; i++) {
            for (int j = 0; j < board.GetLength(i); j++) {
                board[i, j] = null;
            }
        }
        load_fen("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
    }

    public static void load_fen(string FEN) {
        int row = 0;
        int col = 0;
        string[] parts = FEN.Split(' ');
        string position = parts[0];
        string to_play = parts[1];
        for (int i = 0; i < position.Length; i++) {
            char cmd = position[i];
            Piece piece = null;
            if (cmd == 'p')      { piece = new Piece(PieceColor.BLACK, PieceType.PAWN); }
            else if (cmd == 'P') { piece = new Piece(PieceColor.WHITE, PieceType.PAWN); }
            else if (cmd == 'n') { piece = new Piece(PieceColor.BLACK, PieceType.KNIGHT); }
            else if (cmd == 'N') { piece = new Piece(PieceColor.WHITE, PieceType.KNIGHT); }
            else if (cmd == 'b') { piece = new Piece(PieceColor.BLACK, PieceType.BISHOP); }
            else if (cmd == 'B') { piece = new Piece(PieceColor.WHITE, PieceType.BISHOP); }
            else if (cmd == 'k') { piece = new Piece(PieceColor.BLACK, PieceType.KING); }
            else if (cmd == 'K') { piece = new Piece(PieceColor.WHITE, PieceType.KING); }
            else if (cmd == 'q') { piece = new Piece(PieceColor.BLACK, PieceType.QUEEN); }
            else if (cmd == 'Q') { piece = new Piece(PieceColor.WHITE, PieceType.QUEEN); }
            else if (cmd == 'r') { piece = new Piece(PieceColor.BLACK, PieceType.ROOK); }
            else if (cmd == 'R') { piece = new Piece(PieceColor.WHITE, PieceType.ROOK); }
            else if (cmd == '/') { col = 0; row++; }
            else if (char.IsDigit(cmd)) {
                col += cmd - '0';
            }

            if (piece != null) {
                board[row, col++] = piece;
                if (piece.color == PieceColor.WHITE) white_pieces.Add(piece);
                else                                 black_pieces.Add(piece);
            }
        }
        if (to_play[0] == 'w')  turn = PieceColor.WHITE;
        else                    turn = PieceColor.BLACK;
    }

}

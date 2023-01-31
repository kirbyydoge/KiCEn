using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Piece {
    public Coordinate location;
    public PieceColor color;
    public PieceType type;
    public int hash;
    public int first_move;
    public int move_count;
    public bool active;
    public int lookup_index; // To accelerate move search

    public Piece(PieceColor color, PieceType type) {
        this.color = color;
        this.type = type;
        this.active = true;
        first_move = -1;
        move_count = 0;
        hash = get_hash(color, type);
    }

    public static int get_hash(PieceColor color, PieceType type) {
        return ((int)color << 3) + ((int)type);
    }

    public static List<Move> generate_pawn_moves(Piece piece, Coordinate begin) {
        List<Move> moves = new List<Move>();
        Coordinate end;
        Coordinate en_passant_cell;
        Piece target = null;
        Piece en_passant_target = null;
        switch (piece.color) {
            case PieceColor.WHITE:
                // Move up
                end = new Coordinate(begin.rank + 1, begin.file);
                target = ChessGame.get_piece(end);
                if (ChessGame.is_valid_cell(end) && target == null) {
                    moves.Add(new Move(piece, target, begin, end));
                }
                // Opening move
                if (piece.move_count == 0) {
                    end = new Coordinate(begin.rank + 2, begin.file);
                    target = ChessGame.get_piece(end);
                    if (ChessGame.is_valid_cell(end) && target == null && moves.Count > 0) {
                        moves.Add(new Move(piece, target, begin, end));
                    }
                }
                // Captures
                end = new Coordinate(begin.rank + 1, begin.file + 1);
                target = ChessGame.get_piece(end);
                if (target != null && target.color != piece.color) {
                    moves.Add(new Move(piece, target, begin, end));
                }
                // En Passant
                en_passant_cell = new Coordinate(begin.rank, begin.file + 1);
                en_passant_target = ChessGame.get_piece(en_passant_cell);
                if (en_passant_target != null && en_passant_target.type == PieceType.PAWN
                    && en_passant_target.first_move == ChessGame.turn - 1
                    && en_passant_target.color != piece.color) {
                    moves.Add(new Move(piece, en_passant_target, begin, end, true, 0));
                }
                // Captures
                end = new Coordinate(begin.rank + 1, begin.file - 1);
                target = ChessGame.get_piece(end);
                if (target != null && target.color != piece.color) {
                    moves.Add(new Move(piece, target, begin, end));
                }
                // En Passant
                en_passant_cell = new Coordinate(begin.rank, begin.file - 1);
                en_passant_target = ChessGame.get_piece(en_passant_cell);
                if (en_passant_target != null && en_passant_target.type == PieceType.PAWN
                    && en_passant_target.first_move == ChessGame.turn - 1
                    && en_passant_target.color != piece.color) {
                    moves.Add(new Move(piece, en_passant_target, begin, end, true, 0));
                }
                break;
            case PieceColor.BLACK:
                // Move up
                end = new Coordinate(begin.rank - 1, begin.file);
                target = ChessGame.get_piece(end);
                if (ChessGame.is_valid_cell(end) && target == null) {
                    moves.Add(new Move(piece, target, begin, end));
                }
                // Opening move
                if (piece.move_count == 0) {
                    end = new Coordinate(begin.rank - 2, begin.file);
                    target = ChessGame.get_piece(end);
                    if (ChessGame.is_valid_cell(end) && target == null && moves.Count > 0) {
                        moves.Add(new Move(piece, target, begin, end));
                    }
                }
                // Captures
                end = new Coordinate(begin.rank - 1, begin.file + 1);
                target = ChessGame.get_piece(end);
                if (target != null && target.color != piece.color) {
                    moves.Add(new Move(piece, target, begin, end));
                }
                // En Passant
                en_passant_cell = new Coordinate(begin.rank, begin.file + 1);
                en_passant_target = ChessGame.get_piece(en_passant_cell);
                if (en_passant_target != null && en_passant_target.type == PieceType.PAWN
                    && en_passant_target.first_move == ChessGame.turn - 1
                    && en_passant_target.color != piece.color) {
                    moves.Add(new Move(piece, en_passant_target, begin, end, true, 0));
                }
                // Captures
                end = new Coordinate(begin.rank - 1, begin.file - 1);
                target = ChessGame.get_piece(end);
                if (target != null && target.color != piece.color) {
                    moves.Add(new Move(piece, target, begin, end));
                }
                // En Passant
                en_passant_cell = new Coordinate(begin.rank, begin.file - 1);
                en_passant_target = ChessGame.get_piece(en_passant_cell);
                if (en_passant_target != null && en_passant_target.type == PieceType.PAWN
                    && en_passant_target.first_move == ChessGame.turn - 1
                    && en_passant_target.color != piece.color) {
                    moves.Add(new Move(piece, en_passant_target, begin, end, true, 0));
                }
                break;
        }
        return moves;
    }

    public static List<Move> generate_knight_moves(Piece piece, Coordinate begin) {
        Coordinate[] candidates = {
            new Coordinate(begin.rank + 1, begin.file - 2),
            new Coordinate(begin.rank + 2, begin.file - 1),
            new Coordinate(begin.rank + 1, begin.file + 2),
            new Coordinate(begin.rank + 2, begin.file + 1),
            new Coordinate(begin.rank - 1, begin.file - 2),
            new Coordinate(begin.rank - 2, begin.file - 1),
            new Coordinate(begin.rank - 1, begin.file + 2),
            new Coordinate(begin.rank - 2, begin.file + 1)
        };
        List<Move> moves = new List<Move>();
        foreach (Coordinate end in candidates) {
            if (!ChessGame.is_valid_cell(end)) continue;
            Piece target = ChessGame.get_piece_unsafe(end);
            if (target == null || target.color != piece.color) {
                moves.Add(new Move(piece, target, begin, end));
            }
        }
        return moves;
    }

    public static List<Move> generate_bishop_moves(Piece piece, Coordinate begin) {
        List<Move> moves = new List<Move>();
        Coordinate[] diagonals = {
            new Coordinate(1, -1),
            new Coordinate(1, 1),
            new Coordinate(-1, 1),
            new Coordinate(-1, -1)
        };
        foreach (Coordinate dia in diagonals) {
            Coordinate end = begin;
            for (int i = 0; i < 8; i++) {
                end.rank += dia.rank;
                end.file += dia.file;
                if (!ChessGame.is_valid_cell(end)) {
                    break;
                }
                Piece target = ChessGame.get_piece_unsafe(end);
                if (target == null || target.color != piece.color) {
                    moves.Add(new Move(piece, target, begin, end));
                }
                if (target != null) {
                    break;
                }
            }
        }
        return moves;
    }

    public static List<Move> generate_rook_moves(Piece piece, Coordinate begin) {
        List<Move> moves = new List<Move>();
        Coordinate[] diagonals = {
            new Coordinate(0, -1),
            new Coordinate(0, 1),
            new Coordinate(1, 0),
            new Coordinate(-1, 0)
        };
        foreach (Coordinate dia in diagonals) {
            Coordinate end = begin;
            for (int i = 0; i < 8; i++) {
                end.rank += dia.rank;
                end.file += dia.file;
                if (!ChessGame.is_valid_cell(end)) {
                    break;
                }
                Piece target = ChessGame.get_piece_unsafe(end);
                if (target == null || target.color != piece.color) {
                    moves.Add(new Move(piece, target, begin, end));
                }
                if (target != null) {
                    break;
                }
            }
        }
        return moves;
    }

    public static List<Move> generate_queen_moves(Piece piece, Coordinate begin) {
        List<Move> moves = new List<Move>();
        Coordinate[] offsets = {
            new Coordinate(0, -1),
            new Coordinate(0, 1),
            new Coordinate(1, 0),
            new Coordinate(-1, 0),
            new Coordinate(1, -1),
            new Coordinate(1, 1),
            new Coordinate(-1, 1),
            new Coordinate(-1, -1)
        };
        foreach (Coordinate dia in offsets) {
            Coordinate end = begin;
            for (int i = 0; i < 8; i++) {
                end.rank += dia.rank;
                end.file += dia.file;
                if (!ChessGame.is_valid_cell(end)) {
                    break;
                }
                Piece target = ChessGame.get_piece_unsafe(end);
                if (target == null || target.color != piece.color) {
                    moves.Add(new Move(piece, target, begin, end));
                }
                if (target != null) {
                    break;
                }
            }
        }
        return moves;
    }

    public static List<Move> generate_king_moves(Piece piece, Coordinate begin) {
        List<Move> moves = new List<Move>();
        Coordinate[] offsets = {
            new Coordinate(begin.rank, begin.file - 1),
            new Coordinate(begin.rank, begin.file + 1),
            new Coordinate(begin.rank + 1, begin.file),
            new Coordinate(begin.rank - 1, begin.file),
            new Coordinate(begin.rank + 1, begin.file - 1),
            new Coordinate(begin.rank + 1, begin.file + 1),
            new Coordinate(begin.rank - 1, begin.file + 1),
            new Coordinate(begin.rank - 1, begin.file - 1)
        };
        foreach (Coordinate end in offsets) {
            if (!ChessGame.is_valid_cell(end)) {
                continue;
            }
            Piece target = ChessGame.get_piece_unsafe(end);
            if (target == null || target.color != piece.color) {
                moves.Add(new Move(piece, target, begin, end));
            }
        }
        Coordinate[] castle_directions = {
            new Coordinate(0, -1),
            new Coordinate(0, 1)
        };
        foreach (Coordinate dir in castle_directions) {
            if (piece.first_move >= 0 || cell_under_attack(begin, piece.color)) {
                break;
            }
            Coordinate end = begin;  
            for (int i = 0; i < 8; i++) {
                end.file += dir.file;
                if (!ChessGame.is_valid_cell(end)) {
                    break;
                }
                Piece target = ChessGame.get_piece_unsafe(end);
                if (target != null) {
                    if (target.type == PieceType.ROOK && target.color == piece.color && target.move_count == 0) {
                        end.file = begin.file + 2 * dir.file;
                        moves.Add(new Move(piece, target, begin, end, false, dir.file));
                    }
                    else {
                        break;
                    }
                }
                if (cell_under_attack(end, piece.color)) {
                    break;
                }
            }
        }
        return moves;
    }

    public static bool cell_under_attack(Coordinate cell, PieceColor color) {
        Piece attacker = null;
        // Check pawn attacks
        Coordinate[] pawn_offsets = {
            new Coordinate(cell.rank + (color == PieceColor.WHITE ? 1 : -1), cell.file - 1),
            new Coordinate(cell.rank + (color == PieceColor.WHITE ? 1 : -1), cell.file + 1)
        };
        attacker = ChessGame.get_piece(pawn_offsets[0]);
        if (attacker != null && attacker.type == PieceType.PAWN && attacker.color != color) {
            return true;
        }
        attacker = ChessGame.get_piece(pawn_offsets[1]);
        if (attacker != null && attacker.type == PieceType.PAWN && attacker.color != color) {
            return true;
        }
        // Check knight attacks
        Coordinate[] candidates = {
            new Coordinate(cell.rank + 1, cell.file - 2),
            new Coordinate(cell.rank + 2, cell.file - 1),
            new Coordinate(cell.rank + 1, cell.file + 2),
            new Coordinate(cell.rank + 2, cell.file + 1),
            new Coordinate(cell.rank - 1, cell.file - 2),
            new Coordinate(cell.rank - 2, cell.file - 1),
            new Coordinate(cell.rank - 1, cell.file + 2),
            new Coordinate(cell.rank - 2, cell.file + 1)
        };
        foreach (Coordinate cand in candidates) {
            attacker = ChessGame.get_piece(cand);
            if (attacker != null && attacker.type == PieceType.KNIGHT && attacker.color != color) {
                return true;
            }
        }
        // Check diagonals
        Coordinate[] diagonals = {
            new Coordinate(1, -1),
            new Coordinate(1, 1),
            new Coordinate(-1, 1),
            new Coordinate(-1, -1)
        };
        foreach (Coordinate dia in diagonals) {
            Coordinate end = cell;
            for (int i = 0; i < 8; i++) {
                end.rank += dia.rank;
                end.file += dia.file;
                if (!ChessGame.is_valid_cell(end)) {
                    break;
                }
                attacker = ChessGame.get_piece_unsafe(end);
                if (attacker != null) {
                    if (attacker.color != color && (attacker.type == PieceType.BISHOP || attacker.type == PieceType.QUEEN)) {
                        return true;
                    }
                    else {
                        break;
                    }
                }
            }
        }
        // Check axes
        Coordinate[] directions = {
            new Coordinate(0, -1),
            new Coordinate(0, 1),
            new Coordinate(1, 0),
            new Coordinate(-1, 0)
        };
        foreach (Coordinate dir in directions) {
            Coordinate end = cell;
            for (int i = 0; i < 8; i++) {
                end.rank += dir.rank;
                end.file += dir.file;
                if (!ChessGame.is_valid_cell(end)) {
                    break;
                }
                attacker = ChessGame.get_piece_unsafe(end);
                if (attacker != null) {
                    if (attacker.color != color && (attacker.type == PieceType.ROOK || attacker.type == PieceType.QUEEN)) {
                        return true;
                    }
                    else {
                        break;
                    }
                }
            }
        }
        Coordinate[] king_attacks = {
            new Coordinate(cell.rank + 1, cell.file - 1),
            new Coordinate(cell.rank + 1, cell.file + 1),
            new Coordinate(cell.rank - 1, cell.file + 1),
            new Coordinate(cell.rank - 1, cell.file - 1),
            new Coordinate(cell.rank, cell.file - 1),
            new Coordinate(cell.rank, cell.file + 1),
            new Coordinate(cell.rank + 1, cell.file),
            new Coordinate(cell.rank - 1, cell.file)
        };
        foreach (Coordinate dir in king_attacks) {
            Coordinate end = dir;
            if (!ChessGame.is_valid_cell(end)) {
                continue;
            }
            attacker = ChessGame.get_piece_unsafe(end);
            if (attacker != null && attacker.color != color && attacker.type == PieceType.KING) {
                return true;
            }
        }
        return false;
    }
}
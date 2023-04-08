using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardRenderer : MonoBehaviour {

    public int num_ranks = 8;
    public int num_files = 8;
    [Range(0.0f, 10.0f)] public float tile_scale = 1;
    [Range(0.0f, 10.0f)] public float piece_scale = 1;

    public Color color_white_square = new Color(255, 255, 255, 255);
    public Color color_dark_square = new Color(10, 10, 10, 255);
    public Color color_capture_square = new Color(200, 50, 50, 255);
    public Color color_just_moved = new Color(50, 200, 50, 255);

    public Color bg_white_piece = new Color(255, 255, 255, 255);
    public Color bg_dark_piece = new Color(255, 255, 255, 255);

    public GameObject prefab_tile;
    public GameObject prefab_piece;
    public Camera camera_main;

    public Sprite sprite_white_pawn;
    public Sprite sprite_white_bishop;
    public Sprite sprite_white_knight;
    public Sprite sprite_white_rook;
    public Sprite sprite_white_queen;
    public Sprite sprite_white_king;

    public Sprite sprite_black_pawn;
    public Sprite sprite_black_bishop;
    public Sprite sprite_black_knight;
    public Sprite sprite_black_rook;
    public Sprite sprite_black_queen;
    public Sprite sprite_black_king;

    public Sprite sprite_move;

    private float loaded_scale;
    private GameObject[,] tiles;
    private SpriteRenderer[,] tile_renderers;
    private GameObject[,] pieces;
    private SpriteRenderer[,] piece_renderers;
    private Sprite[] piece_map;

    void Start() {
        tiles = new GameObject[num_ranks, num_files];
        tile_renderers = new SpriteRenderer[num_ranks, num_files];
        pieces = new GameObject[num_ranks, num_files];
        piece_renderers = new SpriteRenderer[num_ranks, num_files];
        piece_map = new Sprite[(int)BitPiece.invalid];
        
        piece_map[(int)BitPiece.P] = sprite_white_pawn;
        piece_map[(int)BitPiece.N] = sprite_white_knight;
        piece_map[(int)BitPiece.B] = sprite_white_bishop;
        piece_map[(int)BitPiece.R] = sprite_white_rook;
        piece_map[(int)BitPiece.Q] = sprite_white_queen;
        piece_map[(int)BitPiece.K] = sprite_white_king;

        piece_map[(int)BitPiece.p] = sprite_black_pawn;
        piece_map[(int)BitPiece.n] = sprite_black_knight;
        piece_map[(int)BitPiece.b] = sprite_black_bishop;
        piece_map[(int)BitPiece.r] = sprite_black_rook;
        piece_map[(int)BitPiece.q] = sprite_black_queen;
        piece_map[(int)BitPiece.k] = sprite_black_king;

        create_tiles();
        resize_tiles();
        center_camera();
        render_pieces();
    }

    void Update() {
        if (Mathf.Abs(loaded_scale - tile_scale) > 0.01f) {
            resize_tiles();
            center_camera();
            render_pieces();
        }
    }

    void create_tiles() {
        for (int rank = 0; rank < num_ranks; rank++) {
            for (int file = 0; file < num_files; file++) {
                GameObject spawnedTile = Instantiate(prefab_tile, new Vector3(tile_scale * file, tile_scale * rank, 2), Quaternion.identity);
                spawnedTile.transform.localScale = Vector3.one * tile_scale;
                SpriteRenderer tileRenderer = spawnedTile.GetComponent<SpriteRenderer>();
                tileRenderer.color = (rank + file) % 2 == 0 ? color_dark_square : color_white_square;

                GameObject spawnedPiece = Instantiate(prefab_piece, new Vector3(tile_scale * file, tile_scale * rank, 1), Quaternion.identity);
                SpriteRenderer pieceRenderer = spawnedPiece.GetComponent<SpriteRenderer>();
                pieceRenderer.enabled = true;

                tiles[rank, file] = spawnedTile;
                tile_renderers[rank, file] = tileRenderer;
                pieces[rank, file] = spawnedPiece;
                piece_renderers[rank, file] = pieceRenderer;
            }
        }
        for (int rank = 0; rank < num_ranks; rank++) {
            for (int file = 0; file < num_files; file++) {
                tile_renderers[rank, file].color = (rank + file) % 2 == 0 ? color_dark_square : color_white_square;
            }
        }
        loaded_scale = tile_scale;
    }

    void resize_tiles() {
        for (int rank = 0; rank < num_ranks; rank++) {
            for (int file = 0; file < num_files; file++) {
                tiles[rank, file].transform.position = new Vector3(tile_scale * file, tile_scale * rank, 2);
                pieces[rank, file].transform.position = new Vector3(tile_scale * file, tile_scale * rank, 1);
                tiles[rank, file].transform.localScale = Vector3.one * tile_scale;
            }
        }
        loaded_scale = tile_scale;
    }

    void center_camera() {
        camera_main.transform.position = new Vector3((tile_scale * (num_ranks - 1)) / 2, (tile_scale * (num_files - 1)) / 2, -1);
    }

    public void render_pieces() {
        BitBoardMoveGenerator generator = ChessGame.generator;
        for (int rank = 0; rank < num_ranks; rank++) {
            for (int file = 0; file < num_files; file++) {
                tile_renderers[rank, file].color = (rank + file) % 2 == 0 ? color_dark_square : color_white_square;
                piece_renderers[rank, file].sprite = null;
            }
        }
        for (BitPiece piece = BitPiece.P; piece <= BitPiece.K; piece++) {
            ulong bitboard = generator.bitboards[(int)piece];
            while (bitboard > 0) {
                int square = BitBoardMoveGenerator.pop_lsb(ref bitboard);
                int rank = 7 - square / 8;
                int file = square % 8;
                piece_renderers[rank, file].transform.localScale = Vector3.one * piece_scale;
                piece_renderers[rank, file].sprite = piece_map[(int)piece];
                piece_renderers[rank, file].color = bg_white_piece;
            }
        }
        for (BitPiece piece = BitPiece.p; piece <= BitPiece.k; piece++) {
            ulong bitboard = generator.bitboards[(int)piece];
            while (bitboard > 0) {
                int square = BitBoardMoveGenerator.pop_lsb(ref bitboard);
                int rank = 7 - square / 8;
                int file = square % 8;
                piece_renderers[rank, file].transform.localScale = Vector3.one * piece_scale;
                piece_renderers[rank, file].sprite = piece_map[(int)piece];
                piece_renderers[rank, file].color = bg_dark_piece;
            }
        }
    }

    public void render_moves(List<int> moves) {
        if (moves == null) return;
        for (int i = 0; i < moves.Count; i++) {
            BitBoardMoveGenerator.BitMove move = new BitBoardMoveGenerator.BitMove(moves[i]);
            Coordinate end = new Coordinate(7 - move.target / 8, move.target % 8);
            if (move.is_captures) {
                tile_renderers[end.rank, end.file].color = color_capture_square;
            }
            else {
                piece_renderers[end.rank, end.file].sprite = sprite_move;
                piece_renderers[end.rank, end.file].color = Color.white;
            }
        }
    }

    //!!! ASSERT ChessBoard.get_piece(cell) != null !!!!
    // Should always be called after that anyways
    public Sprite get_sprite(Coordinate cell) {
        return piece_renderers[cell.rank, cell.file].sprite;
    }

    public void disable_cell(Coordinate cell) {
        piece_renderers[cell.rank, cell.file].enabled = false;
    }

    public void enable_cell(Coordinate cell) {
        piece_renderers[cell.rank, cell.file].enabled = true;
    }
}

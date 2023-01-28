using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardRenderer : MonoBehaviour {

    public int num_ranks = 8;
    public int num_files = 8;
    [Range(0.0f, 10.0f)] public float tile_scale = 1;
    [Range(0.0f, 10.0f)] public float piece_scale = 1;

    public Color color_dark_square = new Color(0, 0, 0, 255);
    public Color color_white_square = new Color(255, 255, 255, 255);

    public Color bg_dark_piece = new Color(255, 255, 255, 255);
    public Color bg_white_piece = new Color(255, 255, 255, 255);

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

    private float loaded_scale;
    private GameObject[,] tiles;
    private SpriteRenderer[,] tile_renderers;
    private GameObject[,] pieces;
    private SpriteRenderer[,] piece_renderers;
    private Dictionary<int, Sprite> piece_map;

    void Start() {
        tiles = new GameObject[num_ranks, num_files];
        tile_renderers = new SpriteRenderer[num_ranks, num_files];
        pieces = new GameObject[num_ranks, num_files];
        piece_renderers = new SpriteRenderer[num_ranks, num_files];
        piece_map = new Dictionary<int, Sprite>();

        piece_map[Piece.get_hash(PieceColor.WHITE, PieceType.PAWN)] = sprite_white_pawn;
        piece_map[Piece.get_hash(PieceColor.WHITE, PieceType.KNIGHT)] = sprite_white_knight;
        piece_map[Piece.get_hash(PieceColor.WHITE, PieceType.BISHOP)] = sprite_white_bishop;
        piece_map[Piece.get_hash(PieceColor.WHITE, PieceType.ROOK)] = sprite_white_rook;
        piece_map[Piece.get_hash(PieceColor.WHITE, PieceType.QUEEN)] = sprite_white_queen;
        piece_map[Piece.get_hash(PieceColor.WHITE, PieceType.KING)] = sprite_white_king;

        piece_map[Piece.get_hash(PieceColor.BLACK, PieceType.PAWN)] = sprite_black_pawn;
        piece_map[Piece.get_hash(PieceColor.BLACK, PieceType.KNIGHT)] = sprite_black_knight;
        piece_map[Piece.get_hash(PieceColor.BLACK, PieceType.BISHOP)] = sprite_black_bishop;
        piece_map[Piece.get_hash(PieceColor.BLACK, PieceType.ROOK)] = sprite_black_rook;
        piece_map[Piece.get_hash(PieceColor.BLACK, PieceType.QUEEN)] = sprite_black_queen;
        piece_map[Piece.get_hash(PieceColor.BLACK, PieceType.KING)] = sprite_black_king;

        CreateTiles();
        CenterCamera();
    }

    void Update() {
        if (Mathf.Abs(loaded_scale - tile_scale) > 0.01f) {
            ResizeTiles();
            CenterCamera();
        }
        for (int rank = 0; rank < num_ranks; rank++) {
            for (int file = 0; file < num_files; file++) {
                tile_renderers[rank, file].color = (rank + file) % 2 == 0 ? color_dark_square : color_white_square;
            }
        }
        RenderPieces();
    }

    void CreateTiles() {
        for (int rank = 0; rank < num_ranks; rank++) {
            for (int file = 0; file < num_files; file++) {
                GameObject spawnedTile = Instantiate(prefab_tile, new Vector3(tile_scale * file, tile_scale * rank, 2), Quaternion.identity);
                spawnedTile.transform.localScale = Vector3.one * tile_scale;
                SpriteRenderer tileRenderer = spawnedTile.GetComponent<SpriteRenderer>();
                tileRenderer.color = (rank + file) % 2 == 0 ? color_dark_square : color_white_square;

                GameObject spawnedPiece = Instantiate(prefab_piece, new Vector3(tile_scale * file, tile_scale * rank, 1), Quaternion.identity);
                SpriteRenderer pieceRenderer = spawnedPiece.GetComponent<SpriteRenderer>();
                pieceRenderer.enabled = false;

                tiles[rank, file] = spawnedTile;
                tile_renderers[rank, file] = tileRenderer;
                pieces[rank, file] = spawnedPiece;
                piece_renderers[rank, file] = pieceRenderer;
            }
        }
        loaded_scale = tile_scale;
    }

    void ResizeTiles() {
        for (int rank = 0; rank < num_ranks; rank++) {
            for (int file = 0; file < num_files; file++) {
                tiles[rank, file].transform.position = new Vector3(tile_scale * file, tile_scale * rank, 2);
                pieces[rank, file].transform.position = new Vector3(tile_scale * file, tile_scale * rank, 1);
                tiles[rank, file].transform.localScale = Vector3.one * tile_scale;
            }
        }
        loaded_scale = tile_scale;
    }

    void CenterCamera() {
        camera_main.transform.position = new Vector3((tile_scale * (num_ranks - 1)) / 2, (tile_scale * (num_files - 1)) / 2, -1);
    }

    void RenderPieces() {
        for (int rank = 0; rank < num_ranks; rank++) {
            for (int file = 0; file < num_files; file++) {
                Piece piece = ChessGame.board[rank, file];
                if (piece != null) {
                    piece_renderers[rank, file].enabled = true;
                    piece_renderers[rank, file].transform.localScale = Vector3.one * piece_scale;
                    piece_renderers[rank, file].color = piece.color == PieceColor.WHITE ? bg_white_piece : bg_dark_piece;
                    piece_renderers[rank, file].sprite = piece_map[piece.hash];
                }
                else {
                    piece_renderers[rank, file].enabled = false;
                }
            }
        }
    }
}

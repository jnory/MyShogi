﻿using System;
using System.Drawing;
using System.Windows.Forms;
using MyShogi.App;
using MyShogi.Model.Shogi.Core;
using MyShogi.Model.Test;
using MyShogi.ViewModel;
using ShogiCore = MyShogi.Model.Shogi.Core;

namespace MyShogi.View.Win2D
{
    /// <summary>
    /// 対局盤面などがあるメインウィンドゥ
    /// </summary>
    public partial class MainDialog : Form
    {
        public MainDialog()
        {
            InitializeComponent();

            UpdateMenuItems();
            FindScreenSize();
        }

        // -- 各種定数

        // 盤面素材の画像サイズ
        public static readonly int board_img_width = 1920;
        public static readonly int board_img_height = 1080;

        // 盤面素材における、駒を配置する升の左上。
        public static readonly int board_left = 524;
        public static readonly int board_top = 53;

        // 駒素材の画像サイズ(駒1つ分)
        // これが横に8つ、縦に4つ、計32個並んでいる。
        public static readonly int piece_img_width = 97;
        public static readonly int piece_img_height = 106;

        // 駒台の先手側の右下の座標、駒台の後手側の左上の座標
        public static readonly int hand_img_black_right = 1689;
        public static readonly int hand_img_black_bottom = 1026;
        public static readonly int hand_img_white_left = 230;
        public static readonly int hand_img_white_top = 31;

        // 手駒の表示場所(駒台を左上とする)
        public class HandLocation
        {
            public HandLocation(Piece piece_, int x_, int y_)
            {
                piece = piece_;
                x = x_;
                y = y_;
            }

            public Piece piece;
            public int x;
            public int y;
        };

        /// <summary>
        /// 手駒の表示場所(駒台を左上とする)
        /// </summary>
        private static readonly HandLocation[] hand_location =
        {
            // 10(margin)+96(piece_width)+30(margin)+96(piece_width)+28(margin) = 260(駒台のwidth)
            new HandLocation(Piece.ROOK,10,5),
            new HandLocation(Piece.BISHOP, 135,5),
            new HandLocation(Piece.GOLD,10,100),
            new HandLocation(Piece.SILVER,135,100),
            new HandLocation(Piece.KNIGHT,10,190),
            new HandLocation(Piece.LANCE, 135,190),
            new HandLocation(Piece.PAWN,10,280),
        };

        /// <summary>
        /// 駒台の画面上の位置
        /// </summary>
        private static readonly Point[] hand_table_pos =
        {
            new Point(1431,643), // 先手の駒台
            new Point(229,32),   // 後手の駒台
        };

        /// <summary>
        /// 駒台の幅と高さ
        /// </summary>
        private static int hand_table_width = 260;
        private static int hand_table_height = 388;

        // メニュー高さ。これはClientSize.Heightに含まれてしまうのでこれを加算した分だけ確保しないといけない。
        public static int menu_height = SystemInformation.MenuHeight;

        /// <summary>
        /// スクリーンサイズにぴったり収まるぐらいのウィンドゥサイズを決定する。
        /// </summary>
        public void FindScreenSize()
        {
            // ディスプレイに収まるサイズのスクリーンにする必要がある。
            // プライマリスクリーンを基準にして良いのかどうかはわからん…。
            int w = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
            int h = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;


            float[] scale = new float[]
            {
                // ぴったり収まりそうな画面サイズを探す
                4.0f , 3.0f , 2.5f , 2.0f , 1.75f , 1.5f , 1.25f , 1.0f , 0.75f , 0.5f , 0.25f
            };

            bool is_suitable(int cx /* window width*/ , float s /* scale */)
            {
                // window size = 1440 : 1080 = 4:3
                int clip = (board_img_width - cx) / 2;

                int w2 = (int)((board_img_width - clip * 2) * s);
                int h2 = (int)(board_img_height * s + menu_height);

                // 10%ほど余裕を持って画面に収まるサイズを探す。
                if (w >= (int)(w2 * 1.1f) && h >= (int)(h2 * 1.1f))
                {
                    ClientSize = new Size(w2, h2);
                    clip_x = clip;
                    return true;
                }
                return false;
            }

            foreach (var s in scale)
            {
                // window size = 1620 : 1080 = 3:2
                if (is_suitable(1620, s))
                    break;

                // window size = 1440 : 1080 = 4:3
                if (is_suitable(1440, s))
                    break;

            }
        }

        /// <summary>
        /// メニューのitemを動的に追加する。
        /// 商用版とフリーウェア版とでメニューが異なるのでここで動的に追加する必要がある。
        /// </summary>
        public void UpdateMenuItems()
        {
            var app = TheApp.app;
            var config = app.config;

            // Commercial Version GUI
            bool CV_GUI = config.YaneuraOu2018_GUI_MODE;
            if (CV_GUI)
                Text = "将棋神やねうら王";

            // -- メニューの追加。あとで考える。
            {
                var menu = new MenuStrip();

                //レイアウトロジックを停止する
                SuspendLayout();
                menu.SuspendLayout();

                // 前回設定されたメニューを除去する
                if (old_menu != null)
                    Controls.Remove(old_menu);

                var file_display = new ToolStripMenuItem();
                file_display.Text = "ファイル";
                menu.Items.Add(file_display);
                // あとで追加する。

                var item_display = new ToolStripMenuItem();
                item_display.Text = "表示";
                menu.Items.Add(item_display);

                // -- 「表示」配下のメニュー
                {
                    { // -- 盤面反転
                        var item = new ToolStripMenuItem();
                        item.Text = "盤面反転";
                        item.Checked = config.BoardReverse;
                        item.Click += (sender, e) => { config.BoardReverse = !config.BoardReverse; };

                        item_display.DropDownItems.Add(item);
                    }

                    { // -- 段・筋の画像の選択メニュー

                        var item = new ToolStripMenuItem();
                        item.Text = "筋・段の表示";

                        var item1 = new ToolStripMenuItem();
                        item1.Text = "非表示";
                        item1.Checked = config.BoardNumberImageVersion == 0;
                        item1.Click += (sender, e) => { config.BoardNumberImageVersion = 0; };
                        item.DropDownItems.Add(item1);

                        var item2 = new ToolStripMenuItem();
                        item2.Text = "標準";
                        item2.Checked = TheApp.app.config.BoardNumberImageVersion == 1;
                        item2.Click += (sender, e) => { config.BoardNumberImageVersion = 1; };
                        item.DropDownItems.Add(item2);

                        var item3 = new ToolStripMenuItem();
                        item3.Text = "Chess式";
                        item3.Checked = TheApp.app.config.BoardNumberImageVersion == 2;
                        item3.Click += (sender, e) => { config.BoardNumberImageVersion = 2; };
                        item.DropDownItems.Add(item3);
                        item_display.DropDownItems.Add(item);
                    }

                    if (CV_GUI)
                    {
                        { // -- 盤画像の選択メニュー

                            var item = new ToolStripMenuItem();
                            item.Text = "盤画像";

                            var item1 = new ToolStripMenuItem();
                            item1.Text = "Type1";
                            item1.Checked = config.BoardImageVersion == 1;
                            item1.Click += (sender, e) => { config.BoardImageVersion = 1; };
                            item.DropDownItems.Add(item1);

                            var item2 = new ToolStripMenuItem();
                            item2.Text = "Type2";
                            item2.Checked = config.BoardImageVersion == 2;
                            item2.Click += (sender, e) => { config.BoardImageVersion = 2; };
                            item.DropDownItems.Add(item2);
                            item_display.DropDownItems.Add(item);

                        }

                        { // -- 駒画像の選択メニュー

                            var item = new ToolStripMenuItem();
                            item.Text = "駒画像";

                            var item1 = new ToolStripMenuItem();
                            item1.Text = "一文字駒";
                            item1.Checked = config.PieceImageVersion == 2;
                            item1.Click += (sender, e) => { config.PieceImageVersion = 2; };
                            item.DropDownItems.Add(item1);

                            var item2 = new ToolStripMenuItem();
                            item2.Text = "二文字駒";
                            item2.Checked = TheApp.app.config.PieceImageVersion == 1;
                            item2.Click += (sender, e) => { config.PieceImageVersion = 1; };
                            item.DropDownItems.Add(item2);

                            var item3 = new ToolStripMenuItem();
                            item3.Text = "英文字駒";
                            item3.Checked = TheApp.app.config.PieceImageVersion == 3;
                            item3.Click += (sender, e) => { config.PieceImageVersion = 3; };
                            item.DropDownItems.Add(item3);
                            item_display.DropDownItems.Add(item);
                        }

                    }
                }

#if DEBUG
                // デバッグ用にメニューにテストコードを実行する項目を追加する。
                {
                    var item_debug = new ToolStripMenuItem();
                    item_debug.Text = "デバッグ";

                    var item1 = new ToolStripMenuItem();
                    item1.Text = "DevTest1.Test1()";
                    item1.Click += (sender, e) => { DevTest1.Test1(); };
                    item_debug.DropDownItems.Add(item1);

                    var item2 = new ToolStripMenuItem();
                    item2.Text = "DevTest1.Test2()";
                    item2.Click += (sender, e) => { DevTest1.Test2(); };
                    item_debug.DropDownItems.Add(item2);

                    var item3 = new ToolStripMenuItem();
                    item3.Text = "DevTest1.Test3()";
                    item3.Click += (sender, e) => { DevTest1.Test3(); };
                    item_debug.DropDownItems.Add(item3);

                    var item4 = new ToolStripMenuItem();
                    item4.Text = "DevTest2.Test1()";
                    item4.Click += (sender, e) => { DevTest2.Test1(); };
                    item_debug.DropDownItems.Add(item4);

                    menu.Items.Add(item_debug);
                }
#endif

                Controls.Add(menu);
                //フォームのメインメニューとする
                MainMenuStrip = menu;
                old_menu = menu;

                //レイアウトロジックを再開する
                menu.ResumeLayout(false);
                menu.PerformLayout();
                ResumeLayout(false);
                PerformLayout();
            }

            Invalidate();
        }

        private MenuStrip old_menu { get; set; } = null;

        public MainDialogViewModel ViewModel { get; private set; }

        /// <summary>
        /// 盤面画像を描画するときに左側と右側とをclipする量。
        /// </summary>
        public int clip_x = 0;

        /// <summary>
        /// ViewModelを設定する。
        /// このクラスのインスタンスとは1:1で対応する。
        /// </summary>
        /// <param name="vm"></param>
        public void Bind(MainDialogViewModel vm)
        {
            ViewModel = vm;
        }

        private void MainDialog_Paint(object sender, PaintEventArgs e)
        {
            // ここに盤面を描画するコードを色々書く。(あとで)

            var app = TheApp.app;
            var config = app.config;
            var img = app.imageManager;
            var g = e.Graphics;

            // 盤面を反転させて描画するかどうか
            var reverse = config.BoardReverse;

            // -- 盤面の描画
            {
                var board = img.BoardImg.image;
                var destRect = new Rectangle(0, menu_height, ClientSize.Width, ClientSize.Height - menu_height);
                var sourceRect = new Rectangle(clip_x, 0, board_img_width - clip_x * 2, board_img_height);
                g.DrawImage(board, destRect, sourceRect, GraphicsUnit.Pixel);
            }

            // -- 駒の描画
            {
                // 描画する盤面
                var pos = ViewModel.Pos;

                var piece = img.PieceImg.image;
                scale_x = (double)ClientSize.Width / (board_img_width - clip_x * 2);
                scale_y = (double)(ClientSize.Height - menu_height) / board_img_height;
                offset_x = (int)(-clip_x * scale_x);
                offset_y = menu_height;

                for (Square sq = Square.ZERO; sq < Square.NB; ++sq)
                {
                    var pc = pos.PieceOn(sq);
                    if (pc != Piece.NO_PIECE)
                    {
                        // 盤面反転モードなら、盤面を180度回した升から取得
                        Square sq2 = reverse ? sq.Inv() : sq;
                        int f = 8 - (int)sq2.ToFile();
                        int r = (int)sq2.ToRank();

                        var dstRect = new Rectangle(board_left + piece_img_width * f, board_top + piece_img_height * r,
                            piece_img_width, piece_img_height);

                        // 盤面反転モードなら、駒を先後入れ替える
                        if (reverse)
                            pc = pc ^ Piece.WHITE;

                        var srcRect = new Rectangle(
                            ((int)pc % 8) * piece_img_width,
                            ((int)pc / 8) * piece_img_height,
                            piece_img_width, piece_img_height);

                        Draw(g, piece, dstRect, srcRect);
                    }
                }

                // -- 手駒の描画

                var hand_number = img.HandNumberImg.image;

                for (var c = ShogiCore.Color.ZERO; c < ShogiCore.Color.NB; ++c)
                {
                    Hand h = pos.Hand(reverse ? c.Not() : c);

                    // c側の駒台に描画する。

                    Point p = (c == ShogiCore.Color.BLACK) ?
                        new Point(hand_img_black_right, hand_img_black_bottom) :
                        new Point(hand_img_white_left, hand_img_white_top);


                    // 枚数によって位置が自動調整されるの、わりと見づらいので嫌。
                    // 駒種によって位置固定で良いと思う。


                    //同種の駒が3枚以上になったときに、その駒は1枚だけを表示して、
                    //数字を右肩表示しようと考えていたのですが、例えば、金が2枚、
                    //歩が3枚あるときに、歩だけが数字表示になるのはどうもおかしい気が
                    //するのです。2枚以上は全部数字表示のほうが良いだろう。

                    foreach (var loc in hand_location)
                    {
                        var pc = loc.piece;

                        int count = h.Count(pc);
                        if (count != 0)
                        {
                            // 後手の駒台には後手の駒を描画しなくてはならないのでpcを後手の駒にする。

                            if (c == ShogiCore.Color.WHITE)
                                pc = pc | Piece.WHITE;

                            var srcRect = new Rectangle(
                                ((int)pc % 8) * piece_img_width,
                                ((int)pc / 8) * piece_img_height,
                                piece_img_width, piece_img_height);

                            Rectangle dstRect;
                            
                            if (c == ShogiCore.Color.BLACK)
                                dstRect = new Rectangle(
                                    hand_table_pos[(int)c].X + loc.x,
                                    hand_table_pos[(int)c].Y + loc.y,
                                    piece_img_width, piece_img_height);
                            else
                                // 180度回転させた位置を求める
                                // 後手も駒の枚数は右肩に描画するのでそれに合わせて左右のmarginを調整する。
                                dstRect = new Rectangle(
                                    hand_table_pos[(int)c].X + (hand_table_width  - loc.x - piece_img_width  - 10 ) ,
                                    hand_table_pos[(int)c].Y + (hand_table_height - loc.y - piece_img_height) ,
                                    piece_img_width , piece_img_height);

                            Draw(g, piece, dstRect, srcRect);

                            // 数字の表示
                            if (count > 1)
                            {
                                var srcRect2 = new Rectangle(48 * (count - 1), 0, 48, 48);
                                var dstRect2 = new Rectangle(dstRect.Left + 60, dstRect.Top + 20, 48, 48);
                                Draw(g, hand_number, dstRect2, srcRect2);
                            }
                        }
                    }

                }
            }

            // -- 盤の段・筋を表す数字の表示
            {
                var version = config.BoardNumberImageVersion;
                if (version != 0)
                {
                    var file_img = (!reverse) ? img.BoardNumberImgFile.image : img.BoardNumberImgRevFile.image;
                    if (file_img != null)
                    {
                        var dstRect = new Rectangle(526, 32, file_img.Width, file_img.Height);
                        var srcRect = new Rectangle(0, 0, file_img.Width, file_img.Height);
                        Draw(g, file_img, dstRect, srcRect);
                    }

                    var rank_img = (!reverse) ? img.BoardNumberImgRank.image : img.BoardNumberImgRevRank.image;
                    if (rank_img != null)
                    {
                        var dstRect = new Rectangle(1397, 49, rank_img.Width, rank_img.Height);
                        var srcRect = new Rectangle(0, 0, rank_img.Width, rank_img.Height);
                        Draw(g, rank_img, dstRect, srcRect);
                    }
                }
            }

        }

        // 元画像から画面に描画するときに横・縦方向の縮小率
        private double scale_x;
        private double scale_y;
        private int offset_x;
        private int offset_y;

        /// <summary>
        /// scale_x,scale_y、offset_x,offset_yを用いてアフィン変換してから描画する。
        /// </summary>
        /// <param name="g"></param>
        /// <param name="img"></param>
        /// <param name="destRect"></param>
        /// <param name="sourceRect"></param>
        private void Draw(Graphics g, Image img, Rectangle destRect, Rectangle sourceRect)
        {
            var destRect2 = new Rectangle(
            (int)(destRect.Left * scale_x) + offset_x,
            (int)(destRect.Top * scale_y) + offset_y,
            (int)(destRect.Width * scale_x),
            (int)(destRect.Height * scale_y)
            );
            g.DrawImage(img, destRect2, sourceRect, GraphicsUnit.Pixel);
        }

        private void MainDialog_SizeChanged(object sender, EventArgs e)
        {
            // 画面のフルスクリーン化/ウィンドゥ化がなされたので、OnPaintが呼び出されるようにする。
            Invalidate();
        }

    }
}

﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using System.Numerics;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.Graphics.Canvas.Brushes;
using Windows.UI.Xaml.Media;

namespace Points
{
    public partial class Game
    {
        private const int PLAYER_DRAW = -1;
        private const int PLAYER_NONE = 0;
        private const int PLAYER_HUMAN = 1;
        private const int PLAYER_COMPUTER = 2;
        public int SkillLevel = 5;
        public int SkillDepth = 5;
        public int SkillNumSq = 3;

        //-------------------------------------------------
        public int iScaleCoef = 1;//- коэффициент масштаба
        //public int iBoardSize  ;//- количество клеток квадрата в длинну
        public int iBoardWidth ;//- количество клеток квадрата в длинну
        public int iBoardHeight ;//- количество клеток квадрата в длинну
        //public int iMapSize;//- количество клеток квадрата в длинну - размер всей карты
        //public const int iBoardSizeMin = 5;
        //public const int iBoardSizeMax = 20;

        public float startX = -0.5f, startY = -0.5f;
        public ArrayDots aDots;//Основной массив, где хранятся все поставленные точки. С єтого массива рисуются все точки
        private List<Links> lnks;
        private Dot best_move; //ход который должен сделать комп
        private Dot last_move; //последний ход
        private List<Dot> list_moves; //список ходов
        private int win_player;//переменная получает номер игрока, котрый окружил точки

        private string status = string.Empty;
        public string Status
        {
            get { return status; }
            set { status = value; }
        }
        public List<Dot> ListMoves
        {
            get { return list_moves; }
        }
        public Dot LastMove
        {
            get
            {
                if (last_move == null)//когда выбирается первая точка для хода
                {
                    var random = new Random(DateTime.Now.Millisecond);
                    var q = from Dot d in aDots
                            where d.x <= iBoardWidth / 2 & d.x > iBoardWidth / 3
                                & d.y <= iBoardHeight / 2 & d.y > iBoardHeight / 3
                            orderby (random.Next())
                            select d;

                    last_move = q.First();//это для того чтобы поставить первую точку                
                }
                return last_move;
            }
        }

        public List<Dot> dots_in_region;//записывает сюда точки, которые окружают точку противника
        //=========== цвета, шрифты ===================================================
        public Color colorGamer1 = Colors.Red; //Properties.Settings.Default.Color_Gamer1,
        public Color colorGamer2 = Colors.Blue;//Properties.Settings.Default.Color_Gamer2,
        public Color colorCursor = Color.FromArgb(50, 50, 200, 50);// Properties.Settings.Default.Color_Cursor;
        private float PointWidth = 0.20f;
        public Color colorBoard = Color.FromArgb(255, 150, 200, 200);//(Color.DarkSlateBlue, 0.08f);
        public Color colorDrawBrush = Colors.MediumPurple;
        //public ICanvasBrush SolidBrush;
        ////private SolidBrush drawBrush = new SolidBrush(Colors.MediumPurple);
        //public Font drawFont = new Font("Arial", 0.22f);
        public bool Redraw { get; set; }

        //===============================================================================

        public Point MousePos;

        //statistic
        public float square1;//площадь занятая игроком1
        public float square2;
        public int count_blocked;//счетчик количества окруженных точек
        public int count_blocked1, count_blocked2;
        public int count_dot1, count_dot2;//количество поставленных точек

        private CanvasControl pbxBoard;
        private int _pause = 10;

#if DEBUG
        //public Form f = new Form2();
#endif
        private int iNumberPattern;

#if DEBUG
        Stopwatch stopWatch = new Stopwatch();//для диагностики времени выполнения
        Stopwatch sW_BM = new Stopwatch();
        Stopwatch sW2 = new Stopwatch();
#endif

        public Game(CanvasControl CanvasGame, int boardWidth, int boardHeight)
        {
            pbxBoard = CanvasGame;
            NewGame(boardWidth, boardHeight);
        }
        public void SetLevel(int iLevel = 1)
        {
            switch (iLevel)
            {
                case 0://easy
                    SkillLevel = 10;
                    SkillDepth = 5;
                    SkillNumSq = 3;
                    break;
                case 1://mid
                    SkillLevel = 30;
                    SkillDepth = 10;//20;
                    SkillNumSq = 4;
                    break;
                case 2://hard
                    SkillLevel = 50;
                    SkillDepth = 50;//50;
                    SkillNumSq = 2;//5;
                    break;
            }
            //            Settings.Default.Level=iLevel;
            //            Properties.Settings.Default.Save();
            //#if DEBUG
            //            f.numericUpDown2.Value = SkillDepth;
            //            f.numericUpDown4.Value = SkillNumSq;
            //            f.numericUpDown3.Value = SkillLevel;
            //#endif
        }
        //  ************************************************
        public Dot PickComputerMove(Dot enemy_move)
        {
            #region если первый ход выбираем произвольную соседнюю точку
            if (ListMoves.Count < 2)
            {
                var random = new Random(DateTime.Now.Millisecond);
                var fm = from Dot d in aDots
                         where d.Own == 0 & Math.Sqrt(Math.Pow(Math.Abs(d.x - enemy_move.x), 2) + Math.Pow(Math.Abs(d.y - enemy_move.y), 2)) < 2
                         orderby random.Next()
                         select d;
                return new Dot(fm.First().x, fm.First().y); //так надо чтобы best_move не ссылался на точку в aDots;
            }
            #endregion
            #region  Если ситуация проигрышная - сдаемся          
            var q1 = from Dot d in aDots
                     where d.Own == PLAYER_COMPUTER && (d.Blocked == false)
                     select d;
            var q2 = from Dot d in aDots
                     where d.Own == PLAYER_HUMAN && (d.Blocked == false)
                     select d;
            float res1 = q2.Count();
            float res2 = q1.Count();
            if (res1 / res2 > 2.0)
            {
                return null;
            }

            #endregion


            float s1 = square1; float s2 = square2;
            int pl1 = 0; int pl2 = 0;
            if (enemy_move.Own == PLAYER_HUMAN) { pl1 = PLAYER_HUMAN; pl2 = PLAYER_COMPUTER; }
            else if (enemy_move.Own == PLAYER_COMPUTER) { pl1 = PLAYER_COMPUTER; pl2 = PLAYER_HUMAN; }
            best_move = null;
            int depth = 0;
            var t1 = DateTime.Now.Millisecond;
#if DEBUG
            stopWatch.Start();
#endif
            Dot lm = new Dot(last_move.x, last_move.y);//точка последнего хода
            //проверяем ход который ведет сразу к окружению и паттерны
            //BestMove(pl1, pl2);
            int c1 = 0, c_root = 1000;// , dpth=0;
            lst_best_move.Clear();

#if DEBUG
                //f.lstDbg2.Items.Clear();
                //f.lstDbg1.Items.Clear();
#endif
            Dot dot1 = null, dot2 = null;
            //PLAYER_HUMAN - ставим в параметр - первым ходит игрок1(человек)
            Play(ref best_move, dot1, dot2, PLAYER_HUMAN, PLAYER_COMPUTER, ref depth, ref c1, lm, ref c_root);
            //Play1(ref best_move, dot1, dot2, PLAYER_HUMAN, ref depth, ref c1, lm, ref c_root);
            if (best_move == null)
            {
                //MessageBox.Show("best_move == null");
                var random = new Random(DateTime.Now.Millisecond);
                var q = from Dot d in aDots//любая точка
                        where d.Blocked == false & d.Own == PLAYER_NONE
                        orderby random.Next()
                        select d;

                if (q.Count() > 0) best_move = q.First();
                else return null;
            }

#if DEBUG
            stopWatch.Stop();

            //f.txtDebug.Text = "Skilllevel: " + SkillLevel + "\r\n Общее число ходов: " + depth.ToString() +
            //"\r\n Глубина просчета: " + c_root.ToString() +
            //"\r\n Ход на " + best_move.x + ":" + best_move.y +
            //"\r\n время просчета " + stopWatch.ElapsedMilliseconds.ToString() + " мс";
            stopWatch.Reset();
#endif

            square1 = s1; square2 = s2;

            return new Dot(best_move.x, best_move.y); //так надо чтобы best_move не ссылался на точку в aDots
        }
        //===============================================================================================
        //-----------------------------------Поиск лучшего хода------------------------------------------
        //===============================================================================================
        private Dot BestMove(int pl1, int pl2)
        {
            String strDebug = String.Empty;
            Dot bm;
#if DEBUG
        sW2.Start();
        //f.lblBestMove.Text="CheckMove(pl2,pl1)...";
        
#endif

            #region CheckMove - проверка на окружение
            bm = CheckMove(pl2);
            if (bm != null)
            {
                #region DEBUG
#if DEBUG
                {
                    //f.lstDbg2.Items.Add(bm.x +":"+ bm.y  + " player" + pl2 + " - CheckMove!");
                }
#endif
                #endregion
                return bm;
            }
            bm = CheckMove(pl1);
            if (bm != null)
            {
                #region DEBUG
#if DEBUG
                {
                    //f.lstDbg2.Items.Add(bm.x + ":" + bm.y + " player" + pl1 + " - CheckMove!");
                }
#endif
                #endregion
                return bm;
            }

#if DEBUG
            sW2.Stop();
            strDebug = "CheckMove pl1,pl2 - " + sW2.Elapsed.Milliseconds.ToString();
            //f.txtBestMove.Text = strDebug;
            sW2.Reset();
            //проверяем паттерны
            sW2.Start();
            //f.lblBestMove.Text = "CheckPattern2Move проверяем ходы на два вперед...";
            
#endif
            #endregion
            #region CheckPattern2Move проверяем ходы на два вперед
            List<Dot> empty_dots = aDots.EmptyNeibourDots(pl2);
            List<Dot> lst_dots2;

            foreach (Dot dot in empty_dots)
            {
                if (CheckDot(dot, pl2) == false) MakeMove(dot, pl2);
                lst_dots2 = CheckPattern2Move(pl2);
                foreach (Dot nd in lst_dots2)
                {
                    if (MakeMove(nd, pl2) != 0)
                    {
                        UndoMove(nd);
                        UndoMove(dot);
                        #region DEBUG
#if DEBUG
                        {
                            //f.lstDbg2.Items.Add(dot.x + ":" + dot.y + " player" + pl2 + " - CheckPattern2Move!");
                        }
#endif
                        #endregion
                        return dot;
                    }
                    UndoMove(nd);
                }
                UndoMove(dot);
            }
#if DEBUG
            sW2.Stop();
            strDebug = strDebug + "\r\nCheckPattern2Move(pl2) - " + sW2.Elapsed.Milliseconds.ToString();
            //f.txtBestMove.Text = strDebug;
            sW2.Reset();
            sW2.Start();
            //f.lblBestMove.Text = "CheckPattern_vilochka...";
            
#endif

            #endregion
            #region CheckPattern_vilochka
            bm = CheckPattern_vilochka(pl2);
            if (bm != null & aDots.Contains(bm))
            {
                #region DEBUG
#if DEBUG
                {
                    //f.lstDbg2.Items.Add(bm.x + ":" + bm.y + " player" + pl2 + " - CheckPattern_vilochka " + iNumberPattern);
                }
#endif
                #endregion
                return bm;
            }
            bm = CheckPattern_vilochka(pl1);
            if (bm != null & aDots.Contains(bm))
            {
                #region DEBUG
#if DEBUG

                {
                    //f.lstDbg2.Items.Add(bm.x + ":" + bm.y + " player" + pl1 + " - CheckPattern_vilochka " + iNumberPattern);
                }
#endif
                #endregion
                return bm;
            }

#if DEBUG
            sW2.Stop();
            strDebug = strDebug + "\r\nCheckPattern_vilochka - " + sW2.Elapsed.Milliseconds.ToString();
            //f.txtBestMove.Text = strDebug;
            sW2.Reset();
            sW2.Start();
            //f.lblBestMove.Text = "CheckPatternVilkaNextMove...";
            
#endif
            #endregion
            #region CheckPattern
            bm = CheckPattern(pl2);
            if (bm != null & aDots.Contains(bm))
            {
                #region DEBUG
#if DEBUG
                {
                    //f.lstDbg2.Items.Add(bm.x + ":" + bm.y + " player" + pl2 + " - CheckPattern " + iNumberPattern);
                }
#endif
                #endregion
                if (CheckDot(bm, pl2) == false) return bm;
            }
#if DEBUG
            sW2.Stop();
            strDebug = strDebug + "\r\nCheckPattern(pl2) - " + sW2.Elapsed.Milliseconds.ToString();
            //f.txtBestMove.Text = strDebug;
            sW2.Reset();
            sW2.Start();
            //f.lblBestMove.Text = "CheckPattern(pl1)...";
            
#endif
            #region CheckPatternMove
            bm = CheckPatternMove(pl2);
            if (bm != null & aDots.Contains(bm))
            {
                #region DEBUG
#if DEBUG
                {
                    //f.lstDbg2.Items.Add(bm.x + ":" + bm.y + " player" + pl2 + " - CheckPatternMove " + iNumberPattern);
                }
#endif
                #endregion
                if (CheckDot(bm, pl2) == false) return bm;
            }
            bm = CheckPatternMove(pl1);
            if (bm != null & aDots.Contains(bm))
            {
                #region DEBUG
#if DEBUG
                {
                    //f.lstDbg2.Items.Add(bm.x + ":" + bm.y + " player" + pl2 + " - CheckPatternMove " + iNumberPattern);
                }
#endif
                #endregion
                if (CheckDot(bm, pl1) == false) return bm;
            }
#if DEBUG
            sW2.Stop();
            strDebug = strDebug + "\r\nCheckPatternMove(pl2) - " + sW2.Elapsed.Milliseconds.ToString();
            //f.txtBestMove.Text = strDebug;
            
            sW2.Reset();
#endif

            #endregion

            bm = CheckPattern(pl1);
            if (bm != null & aDots.Contains(bm))
            {
                #region DEBUG
#if DEBUG
                {
                    //f.lstDbg2.Items.Add(bm.x + ":" + bm.y + " player" + pl1 + " - CheckPattern " + iNumberPattern);
                }
#endif
                #endregion
                if (CheckDot(bm, pl2) == false) return bm;
            }

#if DEBUG
            sW2.Stop();
            strDebug = strDebug + "\r\nCheckPattern(pl1) - " + sW2.Elapsed.Milliseconds.ToString();
            //f.txtBestMove.Text = strDebug;
            sW2.Reset();
            sW2.Start();
            //f.lblBestMove.Text = "CheckPatternMove...";
            
#endif
            #endregion
            return null;
        }

        // функция проверяет не делается ли ход в точку, которая на следующем ходу будет окружена
        private bool CheckDot(Dot dot, int Player)
        {
            MakeMove(dot, Player);
            int pl = Player == PLAYER_COMPUTER ? 1 : 2;
            if (win_player == pl || CheckMove(pl) != null)
            {
                UndoMove(dot);
                return true; // да будет окружена
            }
            //нет не будет
            UndoMove(dot);
            return false;
        }
        //==================================================================================================================
        List<Dot> lst_best_move = new List<Dot>();//сюда заносим лучшие ходы
        int res_last_move; //хранит результат хода
                           //===================================================================================================================
        private int Play(ref Dot best_move, Dot move1, Dot move2, int player1, int player2, ref int count_moves,
                               ref int recursion_depth, Dot lastmove, ref int counter_root)//возвращает Owner кто побеждает в результате хода
        {
            #region Debug Skill
#if DEBUG
            //SkillDepth=(int)f.numericUpDown2.Value;
            //SkillNumSq = (int)f.numericUpDown4.Value;
            //SkillLevel = (int)f.numericUpDown3.Value;
#endif
            #endregion
            recursion_depth++;
            if (recursion_depth >= 8)//SkillDepth)
            {
                return 0;
            }
            Dot enemy_move = null;
#if DEBUG
                sW_BM.Start();
#endif
            //проверяем ход который ведет сразу к окружению и паттерны
            best_move = BestMove(player1, player2);
#if DEBUG
                sW_BM.Stop();
                //f.lblBestMove.Text = "BestMove - " + sW_BM.Elapsed.Milliseconds.ToString();
                //
                sW_BM.Reset();
#endif

            if (CheckDot(best_move, player2)) best_move = null;
            if (best_move != null) return PLAYER_COMPUTER;
            var qry = from Dot d in aDots
                      where d.Own == PLAYER_NONE & d.Blocked == false & Math.Abs(d.x - lastmove.x) < SkillNumSq
                                                                    & Math.Abs(d.y - lastmove.y) < SkillNumSq
                      orderby Math.Sqrt(Math.Pow(Math.Abs(d.x - lastmove.x), 2) + Math.Pow(Math.Abs(d.y - lastmove.y), 2))
                      select d;
            Dot[] ad = qry.ToArray();
            int i = ad.Length;
            if (i != 0)
            {
                string sfoo = "";
                #region Cycle
                foreach (Dot d in ad)
                {

                    //player2=1;
                    player2 = player1 == PLAYER_HUMAN ? PLAYER_COMPUTER : PLAYER_HUMAN;
                    //if (count_moves>SkillLevel) break;
                    //**************делаем ход***********************************
                    res_last_move = MakeMove(d, player2);
                    count_moves++;
                    #region проверка на окружение

                    if (win_player == PLAYER_COMPUTER)
                    {
                        best_move = d;
                        UndoMove(d);
                        return PLAYER_COMPUTER;
                    }

                    //если ход в заведомо окруженный регион - пропускаем такой ход
                    if (win_player == PLAYER_HUMAN)
                    {
                        UndoMove(d);
                        continue;
                    }

                    #endregion
                    #region проверяем ход чтобы точку не окружили на следующем ходу
                    sfoo = "CheckMove player" + player1;
                    best_move = CheckMove(player1, false);
                    if (best_move == null)
                    {
                        sfoo = "next move win player" + player2;
                        best_move = CheckMove(player2, false);
                        if (best_move != null)
                        {
                            best_move = d;
                            UndoMove(d);
                            return player2;
                        }
                    }
                    else
                    {
                        UndoMove(d);
                        continue;
                    }
                    #endregion
                    #region Debug statistic
#if DEBUG
                    //if (f.chkMove.Checked) Pause(); //делает паузу если значение поля pause>0
                    //f.lstDbg1.Items.Add(d.Own + " - " + d.x + ":" + d.y);
                    //f.txtDebug.Text = "Общее число ходов: " + count_moves.ToString() +
                    //                   "\r\n Глубина просчета: " + recursion_depth.ToString() +
                    //                   "\r\n проверка вокруг точки " + lastmove +
                    //                   "\r\n move1 " + move1 +
                    //                   "\r\n move2 " + move2 +
                    //                   "\r\n время поиска " + stopWatch.ElapsedMilliseconds;
#endif
                    #endregion
                    //теперь ходит другой игрок ===========================================================================
                    int result = Play(ref enemy_move, move1, move2, player2, player1, ref count_moves, ref recursion_depth, lastmove, ref counter_root);
                    //отменить ход
                    UndoMove(d);
                    recursion_depth--;
#if DEBUG
                    //if (f.lstDbg1.Items.Count > 0) f.lstDbg1.Items.RemoveAt(f.lstDbg1.Items.Count - 1);
#endif
                    if (count_moves > 8)//SkillLevel)
                        return PLAYER_NONE;
                    if (result != 0)
                    {
                        //best_move = enemy_move;
                        best_move = d;
                        return result;
                    }
                    //это конец тела цикла
                }
                #endregion
            }
            return PLAYER_NONE;
        }

        private int FindMove(ref Dot move, Dot last_mv)//возвращает Owner кто побеждает в результате хода
        {
            int depth = 0, counter = 0, counter_root = 1000, own;
            own = PLAYER_HUMAN;//последним ходил игрок
            List<Dot> mvs = new List<Dot>();
            Dot[] ad = null;
            int minX = aDots.MinX();
            int minY = aDots.MinY();
            int maxX = aDots.MaxX();
            int maxY = aDots.MaxY();

            int i = 0;
            do
            {
                if (i == 0)
                {
                    var qry = from Dot d in aDots
                              where d.Own == PLAYER_NONE & d.Blocked == false
                                                        & d.x <= maxX + 1 & d.x >= minX - 1
                                                        & d.y <= maxY + 1 & d.y >= minY - 1
                              orderby d.x
                              select d;
                    ad = qry.ToArray();
                    if (qry.Count() == 0)
                    {
                        foreach (Dot d in mvs)
                        {
                            UndoMove(d);
                        }
                        mvs.Clear();
                        qry = null;
                        i++;
                    }
                }
                else if (i == 1)
                {
                    var qry1 = from Dot d in aDots
                               where d.Own == PLAYER_NONE & d.Blocked == false
                                                         & d.x <= maxX + 1 & d.x >= minX - 1
                                                         & d.y <= maxY + 1 & d.y >= minY - 1
                               orderby d.y descending
                               select d;
                    ad = qry1.ToArray();
                    if (qry1.Count() == 0)
                    {
                        foreach (Dot d in mvs)
                        {
                            UndoMove(d);
                        }
                        mvs.Clear();
                        return 0;
                    }

                }
                depth++;

                if (ad.Length != 0)
                {
                    foreach (Dot d in ad)
                    {
                        counter++;
                        switch (own)
                        {
                            case PLAYER_HUMAN:
                                own = PLAYER_COMPUTER;
                                break;
                            case PLAYER_COMPUTER:
                                own = PLAYER_HUMAN;
                                break;
                        }
                        //ход делает комп, если последним ходил игрок
                        int res_last_move = MakeMove(d, own);
                        mvs.Add(d);
                        //-----показывает проверяемые ходы-----------------------------------------------
#if DEBUG
                        //if (f.chkMove.Checked) Pause();

                        //f.lstDbg1.Items.Add(d.Own + " - " + d.x + ":" + d.y);
                        //f.txtDebug.Text = "Общее число ходов: " + depth.ToString() +
                        //        "\r\n Глубина просчета: " + counter.ToString() +
                        //        "\r\n проверка вокруг точки " + last_move;
#endif
                        //------------------------------------------------------------------------------
                        if (res_last_move != 0 & aDots[d.x, d.y].Blocked)//если ход в окруженный регион
                        {
                            move = null;
                            //UndoMove(d);
                            //return d.Own == PLAYER_HUMAN ? PLAYER_COMPUTER : PLAYER_HUMAN;
                            break;
                        }
                        if (d.Own == 1 & res_last_move != 0)
                        {
                            if (counter < counter_root)
                            {
                                counter_root = counter;
                                move = new Dot(d.x, d.y);
#if DEBUG
                                //f.lstDbg2.Items.Add("Ход на " + move.x + ":" + move.y + "; ход " + counter);
#endif
                            }
                            //UndoMove(d);
                            break;//return PLAYER_HUMAN;//побеждает игрок
                        }
                        else if (d.Own == 2 & res_last_move != 0 | d.Own == 1 & aDots[d.x, d.y].Blocked)
                        {
                            if (counter < counter_root)
                            {
                                counter_root = counter;
                                move = new Dot(d.x, d.y);
#if DEBUG
                                //f.lstDbg2.Items.Add("Ход на " + move.x + ":" + move.y + "; ход " + counter);
#endif
                            }
                            //UndoMove(d);
                            //return PLAYER_COMPUTER;//побеждает компьютер
                            break;
                        }
                        if (depth > SkillLevel * 100)//количество просчитываемых комбинаций
                        {
                            //return PLAYER_NONE;
                            break;
                        }

                    }
                }
            } while (true);

            //return PLAYER_NONE;
        }
        //===============================================================================================================
        private List<Dot> CheckRelation(int index)
        {
            List<Dot> lstDots = new List<Dot>();
            Dot d1, d2;
            var q = from Dot dot in aDots
                    where dot.IndexDot == index & dot.NeiborDots.Count == 1
                    select dot;

            if (q.Count() == 2)
            {
                d1 = q.First();
                d2 = q.Last();
                var qry = from Dot dot in aDots
                          where dot.Own == 0 & aDots.Distance(dot, d1) < 2 & aDots.Distance(dot, d2) < 2
                          select dot;
                return qry.ToList();
            }
            return null;
            //return lstDots;
        }
        //==============================================================================================
        //проверяет ход в результате которого окружение.Возвращает ход который завершает окружение
        private Dot CheckMove(int Owner, bool AllBoard = true)
        {
            var qry = AllBoard ? from Dot d in aDots
                                 where d.Blocked == false && d.Own == 0 &
aDots[d.x + 1, d.y - 1].Blocked == false & aDots[d.x + 1, d.y + 1].Blocked == false & aDots[d.x + 1, d.y - 1].Own == Owner & aDots[d.x + 1, d.y + 1].Own == Owner
| d.Blocked == false & aDots[d.x, d.y + 1].Blocked == false & aDots[d.x, d.y - 1].Blocked == false & d.Own == 0 & aDots[d.x, d.y - 1].Own == Owner & aDots[d.x, d.y + 1].Own == Owner
| d.Blocked == false & d.Own == 0 & aDots[d.x - 1, d.y - 1].Blocked == false & aDots[d.x - 1, d.y + 1].Blocked == false & aDots[d.x - 1, d.y - 1].Own == Owner & aDots[d.x - 1, d.y + 1].Own == Owner
| d.Blocked == false & d.Own == 0 & aDots[d.x - 1, d.y - 1].Blocked == false & aDots[d.x + 1, d.y + 1].Blocked == false & aDots[d.x - 1, d.y - 1].Own == Owner & aDots[d.x + 1, d.y + 1].Own == Owner
| d.Blocked == false & d.Own == 0 & aDots[d.x - 1, d.y + 1].Blocked == false & aDots[d.x + 1, d.y - 1].Blocked == false & aDots[d.x - 1, d.y + 1].Own == Owner & aDots[d.x + 1, d.y - 1].Own == Owner
| d.Blocked == false & d.Own == 0 & aDots[d.x - 1, d.y].Blocked == false & aDots[d.x + 1, d.y].Blocked == false & aDots[d.x - 1, d.y].Own == Owner & aDots[d.x + 1, d.y].Own == Owner

| d.Blocked == false & aDots[d.x - 1, d.y].Blocked == false & aDots[d.x + 1, d.y - 1].Blocked == false & d.Own == 0 & aDots[d.x - 1, d.y].Own == Owner & aDots[d.x + 1, d.y - 1].Own == Owner
| d.Blocked == false & aDots[d.x - 1, d.y].Blocked == false & aDots[d.x + 1, d.y + 1].Blocked == false & d.Own == 0 & aDots[d.x - 1, d.y].Own == Owner & aDots[d.x + 1, d.y + 1].Own == Owner

| d.Blocked == false & aDots[d.x + 1, d.y].Blocked == false & aDots[d.x - 1, d.y - 1].Blocked == false & d.Own == 0 & aDots[d.x + 1, d.y].Own == Owner & aDots[d.x - 1, d.y - 1].Own == Owner
| d.Blocked == false & aDots[d.x + 1, d.y].Blocked == false & aDots[d.x - 1, d.y + 1].Blocked == false & d.Own == 0 & aDots[d.x + 1, d.y].Own == Owner & aDots[d.x - 1, d.y + 1].Own == Owner

| d.Blocked == false & aDots[d.x, d.y + 1].Blocked == false & aDots[d.x - 1, d.y - 1].Blocked == false & d.Own == 0 & aDots[d.x, d.y + 1].Own == Owner & aDots[d.x - 1, d.y - 1].Own == Owner
| d.Blocked == false & aDots[d.x, d.y - 1].Blocked == false & aDots[d.x - 1, d.y + 1].Blocked == false & d.Own == 0 & aDots[d.x, d.y - 1].Own == Owner & aDots[d.x - 1, d.y + 1].Own == Owner

| d.Blocked == false & aDots[d.x, d.y - 1].Blocked == false & aDots[d.x + 1, d.y + 1].Blocked == false & d.Own == 0 & aDots[d.x, d.y - 1].Own == Owner & aDots[d.x + 1, d.y + 1].Own == Owner
| d.Blocked == false & aDots[d.x, d.y + 1].Blocked == false & aDots[d.x + 1, d.y - 1].Blocked == false & d.Own == 0 & aDots[d.x, d.y + 1].Own == Owner & aDots[d.x + 1, d.y - 1].Own == Owner

| d.Blocked == false & aDots[d.x + 1, d.y + 1].Blocked == false & aDots[d.x - 1, d.y + 1].Blocked == false & d.Own == 0 & aDots[d.x + 1, d.y + 1].Own == Owner & aDots[d.x - 1, d.y + 1].Own == Owner
| d.Blocked == false & aDots[d.x - 1, d.y - 1].Blocked == false & aDots[d.x + 1, d.y - 1].Blocked == false & d.Own == 0 & aDots[d.x - 1, d.y - 1].Own == Owner & aDots[d.x + 1, d.y - 1].Own == Owner

| d.Blocked == false & aDots[d.x + 1, d.y + 1].Blocked == false & aDots[d.x + 1, d.y - 1].Blocked == false & d.Own == 0 & aDots[d.x + 1, d.y + 1].Own == Owner & aDots[d.x + 1, d.y - 1].Own == Owner
| d.Blocked == false & aDots[d.x - 1, d.y - 1].Blocked == false & aDots[d.x - 1, d.y + 1].Blocked == false & d.Own == 0 & aDots[d.x - 1, d.y - 1].Own == Owner & aDots[d.x - 1, d.y + 1].Own == Owner
                                 select d :
                    from Dot d in aDots
                    where d.Own == PLAYER_NONE & d.Blocked == false &
                                            Math.Abs(d.x - LastMove.x) < 2 & Math.Abs(d.y - LastMove.y) < 2
                    select d;

            Dot[] ad = qry.ToArray();
            if (ad.Length != 0)
            {
                foreach (Dot d in ad)
                {
                    //делаем ход
                    int result_last_move = (int)MakeMove(d, Owner);
#if DEBUG
                    //if (f.chkMove.Checked) Pause();
#endif
                    //-----------------------------------
                    if (result_last_move != 0 & aDots[d.x, d.y].Blocked == false)
                    {
                        UndoMove(d);
                        return d;
                    }
                    UndoMove(d);
                }
            }
            return null;
        }
        private Dot CheckPatternVilkaNextMove(int Owner)
        {
            var qry = from Dot d in aDots where d.Own == Owner & d.Blocked == false select d;
            Dot dot_ptn;
            Dot[] ad = qry.ToArray();
            if (ad.Length != 0)
            {
                foreach (Dot d in ad)
                {
                    Dot[] dots = new Dot[8] { aDots[d.x + 1, d.y], aDots[d.x - 1, d.y], aDots[d.x, d.y + 1], aDots[d.x, d.y - 1],
                                              aDots[d.x + 1, d.y+1], aDots[d.x - 1, d.y-1], aDots[d.x-1, d.y + 1], aDots[d.x+1, d.y - 1]};
                    foreach (Dot dot_move in dots)
                    {
                        if (dot_move.Blocked == false & dot_move.Own == 0)
                        {
                            //делаем ход
                            int result_last_move = MakeMove(dot_move, Owner);
                            int pl = Owner == PLAYER_COMPUTER ? PLAYER_HUMAN : PLAYER_COMPUTER;
                            Dot dt = CheckMove(pl, false); // проверка чтобы не попасть в капкан
                            if (dt != null)
                            {
                                UndoMove(dot_move);
                                continue;
                            }
                            dot_ptn = CheckPattern_vilochka(d.Own);
#if DEBUG
                                //if (f.chkMove.Checked) Pause();
#endif
                            //-----------------------------------
                            if (dot_ptn != null & result_last_move == 0)
                            {
                                UndoMove(dot_move);
                                return dot_move;
                                //return dot_ptn;
                            }
                            UndoMove(dot_move);
                        }
                    }
                }
            }
            return null;
        }

        private void CheckNextMoves(Dot dot)
        {

            //foreach (Dot d in qry)
            //    {
            //        //**************делаем ход***********************************
            //        d.Own = dot.Own;
            //        res_last_move = MakeMove(d);

            //    }

        }

        public string Statistic()
        {
            var q5 = from Dot d in aDots where d.Own == 1 select d;
            var q6 = from Dot d in aDots where d.Own == 2 select d;
            var q7 = from Dot d in aDots where d.Own == 1 & d.Blocked select d;
            var q8 = from Dot d in aDots where d.Own == 2 & d.Blocked select d;
            return "Игрок1 окружил точек: " + q8.Count() + "; \r\n" +
              "Игрок1 Захваченая площадь: " + square1 + "; \r\n" +
              "Игрок1 точек поставил: " + q5.Count() + "; \r\n" +
              "Игрок2 окружил точек: " + q7.Count() + "; \r\n" +
              "Игрок2 Захваченая площадь: " + square2 + "; \r\n" +
              "Игрок2 точек поставил: " + q6.Count() + "; \r\n";
        }
        public void Statistic(int x, int y)
        {
            if (aDots.Contains(x, y))
            {
#if DEBUG
                //f.txtDotStatus.Text = "Blocked: " + aDots[x, y].Blocked + "\r\n" +
                //              "BlokingDots.Count: " + aDots[x, y].BlokingDots.Count + "\r\n" +
                //              "NeiborDots.Count: " + aDots[x, y].NeiborDots.Count + "\r\n" +
                //              "Rating: " + aDots[x, y].Rating + "\r\n" +
                //              "IndexDot: " + aDots[x, y].IndexDot + "\r\n" +
                //              "IndexRelation: " + aDots[x, y].IndexRelation + "\r\n" +
                //              "Own: " + aDots[x, y].Own + "\r\n" +
                //              "X: " + aDots[x, y].x + "; Y: " + aDots[x, y].y;
#endif
            }
        }
        public int pause
        {
            get
            {
                return _pause;
                //
            }
            set
            {
                _pause = value;
            }
        }
        private void Pause()
        {
#if DEBUG
            if (f.Pause>0)
            {
                
                pbxBoard.Invalidate();
                //System.Threading.Thread.Sleep(f.Pause);
            }
#endif
        }
        public void Pause(int ms)
        {

            pbxBoard.Invalidate();
            //System.Threading.Thread.Sleep(ms);
        }
        public void NewGame(int boardWidth, int boardHeight)
        {
            //iMapSize = iBoardSize * iScaleCoef;
            //aDots = new ArrayDots(iMapSize);

            //iBoardSize = 15;
            aDots = new ArrayDots(boardWidth, boardHeight);
            iBoardWidth = boardWidth;
            iBoardHeight = boardHeight;
            //aDots = new ArrayDots(iBoardSize);
            lnks = new List<Links>();
            dots_in_region = new List<Dot>();
            list_moves = new List<Dot>();
            count_dot1 = 0; count_dot2 = 0;
            startX = -0.5f;
            startY = -0.5f;
            square1 = 0; square2 = 0;
            count_blocked1 = 0; count_blocked2 = 0;
            count_blocked = 0;
            SetLevel(3);
            Redraw = true;
#if DEBUG
        f.Show();

#endif
            pbxBoard.Invalidate();
        }
        public bool GameOver()
        {
            var qry = from Dot d in aDots
                      where d.Own == PLAYER_NONE & d.Blocked == false
                      select d;
            return (qry.Count() == 0);
        }
        private bool DotIsFree(Dot dot, int flg_own)//проверяет заблокирована ли точка. Перед использованием функции надо установить flg_own-владелец проверяемой точки
        {
            dot.Marked = true;

            //if (dot.x == 0 | dot.y == 0 | dot.x == iMapSize - 1 | dot.y == iMapSize - 1)
            if (dot.x == 0 | dot.y == 0 | dot.x == iBoardWidth - 1 | dot.y == iBoardHeight - 1)
            {
                return true;
            }
            Dot[] d = new Dot[4] { aDots[dot.x + 1, dot.y], aDots[dot.x - 1, dot.y], aDots[dot.x, dot.y + 1], aDots[dot.x, dot.y - 1] };
            //--------------------------------------------------------------------------------
            if (flg_own == 0)// если точка не принадлежит никому и рядом есть незаблокированные точки - эта точка считается свободной(незаблокированной)
            {
                var q = from Dot fd in d where fd.Blocked == false select fd;
                if (q.Count() > 0) return true;
            }
            //----------------------------------------------------------------------------------
            for (int i = 0; i < 4; i++)
            {
                if (d[i].Marked == false)
                {
                    if (d[i].Own == 0 | d[i].Own == flg_own | d[i].Own != flg_own & d[i].Blocked & d[i].BlokingDots.Contains(dot) == false)
                    {
                        if (DotIsFree(d[i], flg_own))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        //------------------------------------------------------------------------------------
        public void LinkDots()//устанавливает связь между двумя точками и возвращает массив связей 
        {
            var qry = from Dot d in aDots
                      where d.BlokingDots.Count > 0
                      select d;
            Dot[] dts = qry.ToArray();
            Links l;
            foreach (Dot d in dts)
            {
                for (int i = 0; i < dts.Length; i++)
                {
                    if (d.Equals(dts[i]) == false & d.IsNeiborDots(dts[i]) & d.Blocked == false & dts[i].Blocked == false)
                    {
                        l = new Links(dts[i], d);
                        if (l.LinkExist(lnks.ToArray()) == -1)
                        {
                            lnks.Add(l);
                        }
                    }
                }
            }
        }
        private float SquarePolygon(int nBlockedDots, int nRegionDots)
        {
            return nBlockedDots + nRegionDots / 2.0f - 1;//Формула Пика
        }
        private int count_in_region;
        private int count_blocked_dots;
        //=================================================================================================
        public int MakeMove(Dot dot, int Owner = 0)//Основная функция - ход игрока - возвращает количество окруженных точек
        {
            if (aDots.Contains(dot) == false) return 0;
            if (aDots[dot.x, dot.y].Own == 0)//если точка не занята
            {
                if (Owner == 0) aDots.Add(dot, dot.Own);
                else aDots.Add(dot, Owner);
            }
            //--------------------------------
            int res = CheckBlocked(dot.Own);
            //--------------------------------
            var q = from Dot d in aDots where d.Blocked select d;
            count_blocked_dots = q.Count();
            last_move = dot;//зафиксировать последний ход
            if (res != 0)
            {
                LinkDots();
            }
            return res;
        }
        private int CheckBlocked(int last_moveOwner = 0)//проверяет блокировку точек, маркирует точки которые блокируют, возвращает количество окруженных точек
        {
            int counter = 0;
            var q = from Dot dots in aDots where dots.Own != 0 | dots.Own == 0 & dots.Blocked select dots;
            Dot[] arrDot = q.ToArray();
            switch (last_moveOwner)
            {
                case 1:
                    IEnumerable<Dot> query1 = arrDot.OrderBy(dot => dot.Own == 1);
                    arrDot = query1.ToArray();
                    break;
                case 2:
                    IEnumerable<Dot> query2 = arrDot.OrderBy(dot => dot.Own == 2);
                    arrDot = query2.ToArray();
                    break;
            }
            lst_blocked_dots.Clear(); lst_in_region_dots.Clear();
            foreach (Dot d in arrDot)
            {
                aDots.UnmarkAllDots();
                if (DotIsFree(d, d.Own) == false)
                {
                    //lst_blocked_dots.Clear(); lst_in_region_dots.Clear();
                    if (d.Own != 0) d.Blocked = true;
                    d.IndexRelation = 0;
                    var q1 = from Dot dots in aDots where dots.BlokingDots.Contains(d) select dots;
                    if (q1.Count() == 0)
                    {
                        aDots.UnmarkAllDots();
                        MarkDotsInRegion(d, d.Own);

                        foreach (Dot dr in lst_in_region_dots)
                        {
                            win_player = dr.Own;
                            count_in_region++;
                            foreach (Dot bd in lst_blocked_dots)
                            {
                                if (bd.Own != 0) counter += 1;
                                if (dr.BlokingDots.Contains(bd) == false & bd.Own != 0 & dr.Own != bd.Own)
                                {
                                    dr.BlokingDots.Add(bd);
                                }
                            }
                        }
                    }
                }
                else
                {
                    d.Blocked = false;
                }
            }
            RescanBlocked();

            if (lst_blocked_dots.Count == 0) win_player = 0;
            return lst_blocked_dots.Count;
        }
        private void RescanBlocked()//функция ресканирует списки блокированных точек и устанавливает статус Blocked у єтих точек
        {
            var q = from Dot d in aDots where d.BlokingDots.Count > 0 select d;
            foreach (Dot _d in q)
            {
                foreach (Dot bl_dot in _d.BlokingDots)
                {
                    bl_dot.Blocked = true;
                }
            }
            ScanBlockedFreeDots();
        }
        private List<Dot> lst_blocked_dots = new List<Dot>();//список блокированных точек
        private List<Dot> lst_in_region_dots = new List<Dot>();//список блокирующих точек
        private void MarkDotsInRegion(Dot blocked_dot, int flg_own)//Ставит InRegion=true точкам которые блокируют заданную в параметре точку
        {
            blocked_dot.Marked = true;
            Dot[] dts = new Dot[4] {aDots[blocked_dot.x + 1, blocked_dot.y], aDots[blocked_dot.x - 1, blocked_dot.y],
                                  aDots[blocked_dot.x, blocked_dot.y + 1], aDots[blocked_dot.x, blocked_dot.y - 1]};
            //добавим точки которые попали в окружение
            if (lst_blocked_dots.Contains(blocked_dot) == false)
            {
                lst_blocked_dots.Add(blocked_dot);
            }
            foreach (Dot _d in dts)
            {
                if (_d.Own != 0 & _d.Blocked == false & _d.Own != flg_own)//_d-точка которая окружает
                {
                    //добавим в коллекцию точки которые окружают
                    if (lst_in_region_dots.Contains(_d) == false) lst_in_region_dots.Add(_d);
                }
                else
                {
                    if (_d.Marked == false & _d.Fixed == false)
                    {
                        _d.Blocked = true;
                        MarkDotsInRegion(_d, flg_own);
                    }
                }
            }
        }
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        private void MakeRating()//возвращает массив вражеских точек вокруг заданной точки
        {
            int res;
            var qd = from Dot dt in aDots where dt.Own != 0 & dt.Blocked == false select dt;
            foreach (Dot dot in qd)
            {
                //if (dot.x > 0 & dot.y > 0 & dot.x < iMapSize - 1 & dot.y < iMapSize - 1)
                if (dot.x > 0 & dot.y > 0 & dot.x < iBoardWidth - 1 & dot.y < iBoardHeight - 1)
                {
                    Dot[] dts = new Dot[4] { aDots[dot.x + 1, dot.y], aDots[dot.x - 1, dot.y], aDots[dot.x, dot.y + 1], aDots[dot.x, dot.y - 1] };
                    res = 0;
                    foreach (Dot item in dts)
                    {
                        if (item.Own != 0 & item.Own != dot.Own) res++;
                        else if (item.Own == dot.Own & item.Rating == 0)
                        {
                            res = -1;
                            break;
                        }
                    }
                    dot.Rating = res + 1;//точка без связей получает рейт 1
                }
            }
        }
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        public void ScanBlockedFreeDots()//сканирует не занятые узлы на предмет блокировки
        {
            var q = from Dot d in aDots where d.Own == PLAYER_NONE && d.Blocked == false select d;
            if (q.Count() == 0) return;
            foreach (Dot dot in q)
            {
                Dot[] dts = new Dot[4] {aDots[dot.x + 1, dot.y], aDots[dot.x - 1, dot.y],
                                        aDots[dot.x, dot.y + 1], aDots[dot.x, dot.y - 1]};
                foreach (Dot neibour_dot in dts)
                {
                    if (neibour_dot.Blocked)
                    {
                        dot.Blocked = true;
                        ScanBlockedFreeDots();
                        break;
                    }
                }
            }

        }
        public void ResizeBoard(int boardWidth, int boardHeight)//изменение размера доски
        {
            NewGame(boardWidth, boardHeight);
            pbxBoard.Invalidate();
        }
        public void UndoMove(int x, int y)//поле отмена хода
        {
            Undo(x, y);
        }
        public void UndoMove(Dot dot)//поле отмена хода
        {
            if (dot != null) Undo(dot.x, dot.y);
        }
        private void Undo(int x, int y)//отмена хода
        {
            List<Dot> bl_dot = new List<Dot>();
            List<Links> ln = new List<Links>();
            if (aDots[x, y].Blocked)//если точка была блокирована, удалить ее из внутренних списков у блокирующих точек
            {
                lst_blocked_dots.Remove(aDots[x, y]);
                bl_dot.Add(aDots[x, y]);
                foreach (Dot d in lst_in_region_dots)
                {
                    d.BlokingDots.Remove(aDots[x, y]);
                }
                count_blocked_dots = CheckBlocked();
            }
            if (aDots[x, y].BlokingDots.Count > 0)
            {
                //снимаем блокировку с точки bd, которая была блокирована UndoMove(int x, int y)
                foreach (Dot d in aDots[x, y].BlokingDots)
                {
                    bl_dot.Add(d);
                }
            }

            foreach (Dot d in bl_dot)
            {
                foreach (Links l in lnks)//подготовка связей которые блокировали точку
                {
                    if (l.Dot1.BlokingDots.Contains(d) | l.Dot2.BlokingDots.Contains(d))
                    {
                        ln.Add(l);
                    }
                }
                //удаляем из списка блокированных точек
                foreach (Dot bd in aDots)
                {
                    if (bd.BlokingDots.Count > 0)
                    {
                        bd.BlokingDots.Remove(d);
                    }
                }
                //восстанавливаем связи у которых одна из точек стала свободной
                var q_lnks = from lnk in lnks
                             where lnk.Dot1.x == d.x & lnk.Dot1.y == d.y | lnk.Dot2.x == d.x & lnk.Dot2.y == d.y
                             select lnk;
                foreach (Links l in q_lnks)
                {
                    l.Dot1.Blocked = false;
                    l.Dot2.Blocked = false;
                }

            }
            //удаляем связи
            foreach (Links l in ln)
            {
                lnks.Remove(l);
            }
            ln = null;
            bl_dot = null;

            aDots.Remove(x, y);
            count_blocked_dots = CheckBlocked();
            ScanBlockedFreeDots();
            aDots.UnmarkAllDots();
            LinkDots();
            last_move = list_moves.Count == 0 ? null : list_moves.Last();
        }

        #region RENDER
        public void DrawGame(CanvasControl canvasCtrl, CanvasDrawingSession drawingSession)//отрисовка хода игры
        {
            //if (антиалToolStripMenuItem.Checked)
            //{
            drawingSession.Antialiasing = CanvasAntialiasing.Antialiased;
            //}
            //Устанавливаем масштаб
            
            SetScale(drawingSession, (int)canvasCtrl.ActualWidth, (int)canvasCtrl.ActualHeight,
                startX, startX + iBoardWidth, startY, iBoardHeight + startY);

            //Рисуем доску
            DrawBoard(drawingSession);
            //Рисуем точки
            DrawPoints(drawingSession);
            //Отрисовка курсора
            //drawingSession.FillEllipse(MousePos.X - PointWidth, MousePos.Y - PointWidth, PointWidth * 2, PointWidth * 2, Color.FromArgb(30, colorCursor.R, colorCursor.G, colorCursor.B));
            //drawingSession.FillEllipse(MousePos.X - PointWidth/2, MousePos.Y - PointWidth/2, PointWidth , PointWidth, Colors.DarkSalmon);
            //drawingSession.DrawEllipse(new Pen(Color.FromArgb(50, colorCursor), 0.05f), MousePos.X - PointWidth, MousePos.Y - PointWidth, PointWidth * 2, PointWidth * 2);
            //Отрисовка замкнутого региона игрока1
            DrawLinks(drawingSession);

            drawingSession.DrawLine(0, 100,100, 0, colorBoard, 5.0f);
        }
        public void DrawBoard(CanvasDrawingSession drawingSession)//рисуем доску из клеток
        {
            for (float i = 0; i < iBoardWidth; i++)
            {
                Color drB = i == 0 ? Colors.MediumSeaGreen : colorDrawBrush;
                drawingSession.DrawLine(i + startX + 0.5f, startY + 0.5f, i + startX + 0.5f, iBoardHeight + startY - 0.5f, colorBoard, 0.08f);
            }
            for (float i = 0; i < iBoardHeight; i++)
            {
                Color drB = i == 0 ? Colors.MediumSeaGreen : colorDrawBrush;
                drawingSession.DrawLine(startX + 0.5f, i + startY + 0.5f, iBoardWidth + startX - 0.5f, i + startY + 0.5f, colorBoard, 0.08f);
            }

        }
        public void DrawLinks(CanvasDrawingSession drawingSession)//отрисовка связей
        {
            if (lnks != null)
            {
                Color colorGamer;
                for (int i = 0; i < lnks.Count; i++)
                {
                    if (lnks[i].Dot1.Blocked)//0.1f
                    {
                        colorGamer = lnks[i].Dot1.Own == 1 ? Color.FromArgb(130, colorGamer1.R, colorGamer1.G, colorGamer1.B) :
                                                             Color.FromArgb(130, colorGamer2.R, colorGamer2.G, colorGamer2.B);
                        drawingSession.DrawLine(lnks[i].Dot1.x, lnks[i].Dot1.y, lnks[i].Dot2.x, lnks[i].Dot2.y, colorGamer, 0.1f);
                    }
                    else
                    {
                        colorGamer = lnks[i].Dot1.Own == 1 ? colorGamer1 : colorGamer2;
                        drawingSession.DrawLine(lnks[i].Dot1.x, lnks[i].Dot1.y, lnks[i].Dot2.x, lnks[i].Dot2.y, colorGamer, 0.1f);
                    }
                }
            }
            }
        public void DrawPoints(CanvasDrawingSession drawingSession)//рисуем поставленные точки
        {
            //отрисовываем поставленные точки
            if (aDots.Count > 0)
            {
                foreach (Dot p in aDots)
                {
                    switch (p.Own)
                    {
                        case 1:
                            SetColorAndDrawDots(drawingSession, colorGamer1, p);
                            break;
                        case 2:
                            SetColorAndDrawDots(drawingSession, colorGamer2, p);
                            break;
                        case 0:
                            if (p.PatternsEmptyDot) SetColorAndDrawDots(drawingSession, Colors.Bisque, p);
                            if (p.PatternsAnyDot) SetColorAndDrawDots(drawingSession, Colors.DarkOrange, p);
                            break;
                    }
                }
            }
        }
        private void SetColorAndDrawDots(CanvasDrawingSession drawingSession, Color colorGamer, Dot p) //Вспомогательная функция для DrawPoints. Выбор цвета точки в зависимости от ее состояния и рисование элипса
        {

            Color c;
            if (p.Blocked)
            {
                drawingSession.FillEllipse(p.x, p.y, PointWidth , PointWidth , Color.FromArgb(130, colorGamer.R, colorGamer.G, colorGamer.B));
            }
            else if (last_move != null && p.x == last_move.x & p.y == last_move.y)//точка последнего хода должна для удоиства выделяться
            {
                drawingSession.FillEllipse(p.x , p.y , PointWidth , PointWidth , Color.FromArgb(140, colorGamer.R, colorGamer.G, colorGamer.B));
                drawingSession.DrawEllipse(p.x , p.y, PointWidth/2, PointWidth/2, Colors.WhiteSmoke, 0.05f);
                drawingSession.DrawEllipse(p.x , p.y , PointWidth , PointWidth , colorGamer, 0.08f);
            }
            else
            {
                int G = colorGamer.G > 50 ? colorGamer.G - 50 : 120;
                c = p.BlokingDots.Count > 0 ? Color.FromArgb(255, colorGamer.R, colorGamer.G, colorGamer.B) : colorGamer;
                drawingSession.FillEllipse(p.x , p.y , PointWidth , PointWidth , colorGamer);
                drawingSession.DrawEllipse(p.x , p.y , PointWidth , PointWidth , c, 0.08f);
            }
            #region Editor patterns
            //if (p.PatternsEmptyDot)
            //{
            //    drawingSession.FillEllipse(new SolidBrush(Color.FromArgb(100, Color.WhiteSmoke)), p.x - PointWidth, p.y - PointWidth, PointWidth * 2, PointWidth * 2);
            //    drawingSession.DrawEllipse(new Pen(Color.Transparent, 0.08f), p.x - PointWidth, p.y - PointWidth, PointWidth * 2, PointWidth * 2);
            //}
            //if (p.PatternsMoveDot)
            //{
            //    //gr.FillEllipse(new SolidBrush(Color.FromArgb(50, Color.Plum)), p.x - PointWidth, p.y - PointWidth, PointWidth * 2, PointWidth * 2);
            //    drawingSession.DrawEllipse(new Pen(Color.LimeGreen, 0.08f), p.x - PointWidth, p.y - PointWidth, PointWidth * 2, PointWidth * 2);
            //}
            //if (p.PatternsFirstDot)
            //{
            //    //gr.FillEllipse(new SolidBrush(Color.FromArgb(50, Color.ForestGreen)), p.x - PointWidth, p.y - PointWidth, PointWidth * 2, PointWidth * 2);
            //    drawingSession.DrawEllipse(new Pen(Color.DarkSeaGreen, 0.08f), p.x - PointWidth, p.y - PointWidth, PointWidth * 2, PointWidth * 2);
            //}
            //if (p.PatternsAnyDot)
            //{
            //    drawingSession.FillEllipse(new SolidBrush(Color.Yellow), p.x - PointWidth, p.y - PointWidth, PointWidth * 2, PointWidth * 2);
            //    drawingSession.DrawEllipse(new Pen(Color.Orange, 0.08f), p.x - PointWidth, p.y - PointWidth, PointWidth * 2, PointWidth * 2);
            //}
            #endregion

        }
        //Matrix _transform = new Matrix();//матрица для преобразования координат точек в заданном масштабе
        //Matrix _transform = new Matrix();//матрица для преобразования координат точек в заданном масштабе
        Matrix3x2 _transform;
        /// <summary>
        /// функция масштабирования, устанавливает массштаб
        /// </summary>
        /// <param name="gr - CanvasDrawingSession"></param>
        /// <param name="gr_width - ширина клиентской области"></param>
        /// <param name="gr_height - длина клиентской области"></param>
        /// <param name="left_x"></param>
        /// <param name="right_x"></param>
        /// <param name="top_y"></param>
        /// <param name="bottom_y"></param>
        private void SetScale(CanvasDrawingSession gr, int gr_width, int gr_height, float left_x, float right_x, float top_y, float bottom_y)
        {
            Matrix3x2 matrixTemp = gr.Transform;
            matrixTemp = Matrix3x2.CreateScale(new Vector2(gr_width / (right_x - left_x), gr_height / (bottom_y - top_y)), 
                                               new Vector2(left_x, top_y));
            gr.Transform = matrixTemp;
            _transform = matrixTemp;
        }
        public Point TranslateCoordinates(Point MousePos)
        {
            Matrix3x2 transform;
            Matrix3x2.Invert(_transform, out transform);
            Vector2 v = Vector2.Transform(new Vector2((float)MousePos.X, (float)MousePos.Y), transform);
            Point result = new Point((int)Math.Round(v.X), (int)Math.Round(v.Y));
            return result;
        }

        #endregion

        //=========================================================================
#if DEBUG
        public void MoveDebugWindow(int top, int left, int width)
        {
            f.Top = top;
            f.Left = left + width;
        }
        #region Pattern Editor
        private List<Dot> lstPat;
        public List<Dot> ListPatterns
        {
            get { return lstPat; }
        }

        public bool Autoplay
        {
        
            get { return f.rbtnHand.Checked; }
            //set { f.rbtnHand.Checked = value; }
        
        }


        public bool PE_FirstDot
        {
            get { return f.tlsТочкаОтсчета.Checked; }
            set { f.tlsТочкаОтсчета.Checked = value; }
        }
        public bool PE_EmptyDot
        {
            get { return f.tlsПустая.Checked; }
            set { f.tlsПустая.Checked = value; }

        }

        public bool PE_AnyDot
        {
            get { return f.tlsКромеВражеской.Checked; }
            set { f.tlsКромеВражеской.Checked = value; }

        }
        public bool PE_MoveDot
        {
            get { return f.tlsТочкаХода.Checked; }
            set { f.tlsТочкаХода.Checked = value; }

        }
        public bool PE_On
        {
            get
            {
                if (f.tlsEditPattern.Checked & lstPat==null) lstPat = new List<Dot>();
                return f.tlsEditPattern.Checked;

            }
            set { f.tlsEditPattern.Checked = value; }
        }
        public void MakePattern()//сохраняет паттерн в текстовое поле
        {
            string s, strdX, strdY, sWhere = "", sMove = "";
            int dx, dy, ind;
            ind = lstPat.FindIndex(
                delegate (Dot dt)
                {
                    return dt.PatternsFirstDot == true;
                });
            var random = new Random(DateTime.Now.Millisecond);
            string n = random.Next(1, 1000).ToString();
            for (int i = 0; i < lstPat.Count; i++)
            {
                string own = "";
                if (lstPat[ind].Own == lstPat[i].Own) own = "== Owner";
                if (lstPat[ind].Own != lstPat[i].Own) own = "== enemy_own";
                if (lstPat[i].Own == 0 & lstPat[i].PatternsAnyDot==false) own = " == 0";
                if (lstPat[i].PatternsAnyDot) own = " != enemy_own";

                dx = lstPat[i].x - lstPat[ind].x;
                if (dx == 0) strdX = "";
                else if (dx > 0) strdX = "+" + dx.ToString();
                else strdX = dx.ToString();

                dy = lstPat[i].y - lstPat[ind].y;
                if (dy == 0) strdY = "";
                else if (dy > 0) strdY = "+" + dy.ToString();
                else strdY = dy.ToString();

                if ((dx == 0 & dy == 0) == false) sWhere += " && aDots[d.x" + strdX + ", d.y" + strdY + "].Own " + own + " && aDots[d.x" + strdX + ", d.y" + strdY + "].Blocked == false \r\n";

                if (lstPat[i].PatternsMoveDot)
                {
                    sMove = " if (pat" + n + ".Count() > 0) return new Dot(pat" + n + ".First().x" + strdX + "," + "pat" + n + ".First().y" + strdY + ");";
                }
            }
            s = "iNumberPattern = " + n + "; \r\n";
            s += "var pat" + n + " = from Dot d in aDots where d.Own == Owner \r\n" + sWhere + "select d; \r\n" + sMove + "\r\n";
            n += "_2";
            sWhere = ""; sMove = "";
            for (int i = 0; i < lstPat.Count ; i++)
            {
                string own = "";
                if (lstPat[ind].Own == lstPat[i].Own) own = "== Owner";
                if (lstPat[ind].Own != lstPat[i].Own) own = "== enemy_own";
                if (lstPat[i].Own == 0 & lstPat[i].PatternsAnyDot == false) own = " == 0";
                if (lstPat[i].PatternsAnyDot) own = " != enemy_own";

                dx = lstPat[ind].x - lstPat[i].x;
                if (dx == 0) strdX = "";
                else if (dx > 0) strdX = "+" + dx.ToString();
                else strdX = dx.ToString();

                dy = lstPat[ind].y - lstPat[i].y;
                if (dy == 0) strdY = "";
                else if (dy > 0) strdY = "+" + dy.ToString();
                else strdY = dy.ToString();
                if ((dx == 0 & dy == 0) == false) sWhere += " && aDots[d.x" + strdX + ", d.y" + strdY + "].Own " + own + " && aDots[d.x" + strdX + ", d.y" + strdY + "].Blocked == false \r\n";
                if (lstPat[i].PatternsMoveDot)
                {
                    sMove = " if (pat" + n + ".Count() > 0) return new Dot(pat" + n + ".First().x" + strdX + "," + "pat" + n + ".First().y" + strdY + ");";
                }

            }
            s += "//180 Rotate=========================================================================================================== \r\n";
            s += "var pat" + n + " = from Dot d in aDots where d.Own == Owner \r\n" + sWhere + "select d; \r\n" + sMove + "\r\n";
            
            n += "_3";
            sWhere = ""; sMove = "";
            List<Dot> l =RotateMatrix(90);
            for (int i = 0; i < l.Count ; i++)
            {
                string own = "";
                if (l[ind].Own == l[i].Own) own = "== Owner";
                if (l[ind].Own != l[i].Own) own = "== enemy_own";
                if (l[i].Own == 0 & l[i].PatternsAnyDot == false) own = " == 0";
                if (l[i].PatternsAnyDot) own = " != enemy_own";

                dx = l[ind].x - l[i].x;
                if (dx == 0) strdX = "";
                else if (dx > 0) strdX = "+" + dx.ToString();
                else strdX = dx.ToString();

                dy = l[ind].y - l[i].y;
                if (dy == 0) strdY = "";
                else if (dy > 0) strdY = "+" + dy.ToString();
                else strdY = dy.ToString();
                if ((dx == 0 & dy == 0) == false) sWhere += " && aDots[d.x" + strdX + ", d.y" + strdY + "].Own " + own + " && aDots[d.x" + strdX + ", d.y" + strdY + "].Blocked == false \r\n";
                if (l[i].PatternsMoveDot)
                {
                    sMove = " if (pat" + n + ".Count() > 0) return new Dot(pat" + n + ".First().x" + strdX + "," + "pat" + n + ".First().y" + strdY + ");";
                }
            }
            s += "//--------------Rotate on 90----------------------------------- \r\n";
            s += "var pat" + n + " = from Dot d in aDots where d.Own == Owner \r\n" + sWhere + "select d; \r\n" + sMove + "\r\n";
            n += "_4";
            sWhere = ""; sMove = "";
            for (int i = 0; i < l.Count ; i++)
            {
                string own = "";
                if (l[ind].Own == l[i].Own) own = "== Owner";
                if (l[ind].Own != l[i].Own) own = "== enemy_own";
                if (l[i].Own == 0 & l[i].PatternsAnyDot == false) own = " == 0";
                if (l[i].PatternsAnyDot) own = " != enemy_own";

                dx = l[i].x - l[ind].x;
                if (dx == 0) strdX = "";
                else if (dx > 0) strdX = "+" + dx.ToString();
                else strdX = dx.ToString();

                dy = l[i].y - l[ind].y;
                if (dy == 0) strdY = "";
                else if (dy > 0) strdY = "+" + dy.ToString();
                else strdY = dy.ToString();
                if ((dx == 0 & dy == 0) == false) sWhere += " && aDots[d.x" + strdX + ", d.y" + strdY + "].Own " + own + " && aDots[d.x" + strdX + ", d.y" + strdY + "].Blocked == false \r\n";
                if (l[i].PatternsMoveDot)
                {
                    sMove = " if (pat" + n + ".Count() > 0) return new Dot(pat" + n + ".First().x" + strdX + "," + "pat" + n + ".First().y" + strdY + ");";
                }
            }
            s += "//--------------Rotate on 90 - 2----------------------------------- \r\n";
            s += "var pat" + n + " = from Dot d in aDots where d.Own == Owner \r\n" + sWhere + "select d; \r\n" + sMove + "\r\n";
            s += "//============================================================================================================== \r\n";
            f.txtDebug.Text = s;
            MessageBox.Show("Into clipboard!");
            Clipboard.Clear();
            Clipboard.SetText(s);

            lstPat.Clear();
            f.tlsEditPattern.Checked=false;
            aDots.UnmarkAllDots();
        }

        private List<Dot> RotateMatrix(int ungle)
        {
        Array m = new Array[lstPat.Count];
        List<Dot> l = new List<Dot>(lstPat.Count);
            if(ungle==90)
            {
                foreach(Dot d in lstPat)
                {
                    int x=d.x; 
                    int y = d.y;
                    d.x = y; d.y = x;
                    l.Add(d);
                }        
            }
            return l;
        }

        #endregion
#endif
        //==========================================================================
        #region SAVE_LOAD Game
        //public string path_savegame = Application.CommonAppDataPath + @"\dots.dts";
        //public void SaveGame()
        //{
        //    try
        //    {
        //        // создаем объект BinaryWriter
        //        using (BinaryWriter writer = new BinaryWriter(File.Open(path_savegame, FileMode.Create)))
        //        {

        //		for (int i = 0; i < list_moves.Count; i++)
        //   			{
        //                writer.Write((byte)list_moves[i].x);
        //                writer.Write((byte)list_moves[i].y);
        //                writer.Write((byte)list_moves[i].Own);
        //        	}
        //    	}
        //    }
        //    catch (Exception e)
        //    {
        //        MessageBox.Show(e.Message);
        //    }
        //}
        //public void LoadGame()
        //{
        //    aDots.Clear();
        //    lnks.Clear();
        //    list_moves.Clear();
        //    Dot d=null;
        //    try
        //    {
        //        // создаем объект BinaryReader
        //        BinaryReader reader = new BinaryReader(File.Open(path_savegame, FileMode.Open));
        //        // пока не достигнут конец файла считываем каждое значение из файла
        //        while (reader.PeekChar() > -1)
        //        {
        //            d = new Dot((int)reader.ReadByte(), (int)reader.ReadByte(), (int)reader.ReadByte());
        //            MakeMove(d,d.Own);
        //            list_moves.Add(aDots[d.x,d.y]);
        //        }
        //        last_move = d;
        //        //CheckBlocked();//проверяем блокировку
        //        LinkDots();//восстанавливаем связи между точками
        //        RescanBlocked();
        //        //ScanBlockedFreeDots();
        //        reader.Close();
        //    }
        //    catch (Exception e)
        //    {
        //        MessageBox.Show(e.Message);
        //    }

        //}
        #endregion


        //struct Dots_sg//структура для сохранения игры в файл
        //{
        //    public byte x;
        //    public byte y;
        //    public byte Own;
        //    public Dots_sg(int X, int Y, int Owner)
        //    {
        //        x = (byte)X;
        //        y = (byte)Y;
        //        Own = (byte)Owner;
        //    }
        //}
    }

}

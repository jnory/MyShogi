﻿using System;
using System.Windows.Forms;
using MyShogi.Model.Common.Utility;
using MyShogi.View.Win2D;

namespace MyShogi.App
{
    /// <summary>
    /// TheAppの、MessageShow()まわりだけを切り離した。
    /// </summary>
    public partial class TheApp
    {
        /// <summary>
        /// 最前面に来るようにしてMessageBox.Show(text)を呼び出す。
        /// </summary>
        /// <param name="text"></param>
        public DialogResult MessageShow(string text, MessageShowType type)
        {
            var caption = type.Pretty();
            var icon = type.ToIcon();
            var buttons = type.ToButtons();

            var show = new Func<Form,DialogResult>((parent) =>
            {
                if (type == MessageShowType.Exception)
                {
                    // 例外のときだけ、コピペできるように専用のDialogを出す。
                    using (var dialog = new ExceptionDialog())
                    {
                        dialog.SetMessage(text);
                        // 専用のダイアログなのでメインウインドウに対してセンタリングも楽ちん
                        if (parent != null)
                            FormLocationUtility.CenteringToThisForm(dialog, parent);

                        FontUtility.ReplaceFont(dialog);
                        dialog.ShowDialog();
                    }
                    return DialogResult.OK;
                }
                else
                {
                    // これセンタリングしたいのだが、メッセージフックしないと不可能。
                    // cf.
                    //   オーナーウィンドウの中央にメッセージボックスを表示する (C#プログラミング)
                    //   https://www.ipentec.com/document/csharp-show-message-box-in-center-of-owner-window

                    if (parent != null)
                        return MessageBox.Show(parent, text, caption, buttons, icon);
                    else
                        return MessageBox.Show(text, caption, buttons, icon);
                }
            });

            if (mainForm != null && mainForm.IsHandleCreated && !mainForm.IsDisposed)
            {
                if (mainForm.InvokeRequired)
                {
                    try
                    {
                        var result = mainForm.BeginInvoke(new Func<DialogResult>(() => { return show(mainForm); }));
                        return (DialogResult)mainForm.EndInvoke(result); // これで結果が返るまで待つはず..
                        // ここでウィンドウが破棄される可能性があるのでEndInvoke()が成功するとは限らない。
                    }
                    catch
                    {
                        return DialogResult.OK;
                    }
                }
                else
                    return show(mainForm);
            }
            else
            {
                // メインウインドウ、解体後。
                return show(null);
            }

        }

        /// <summary>
        /// 例外をダイアログで表示する用。
        /// </summary>
        /// <param name="ex"></param>
        public void MessageShow(Exception ex , bool exit = true)
        {
            if (exit)
            {
                var message = "例外が発生しましたので終了します。\r\n" + ex.Pretty();

                // Mono環境でコマンドラインからコマンドを叩いているときにコンソールに
                // 例外内容が表示されたほうがデバッグしやすいのでConsoleにも出力する。
                Console.WriteLine(message);

                MessageShow(message , MessageShowType.Exception);
                ApplicationExit();
            } else
            {
                MessageShow("例外が発生しました。\r\n" + ex.Pretty() , MessageShowType.Exception);
            }
        }

        public void ApplicationExit()
        {
            // DockWindowがあると閉じるのを阻害する。(window closingに対してCancelしているので)
            // DockWindowはメインウインドウにぶら下がっているはずなので、メインウインドウを終了させてしまう。

            Exiting = true; // このフラグをDockWindowから見に来ている。

            Application.Exit(); // 終了させてしまう。
        }

        /// <summary>
        /// App.ApplicationExit()が呼び出されたあとであるかのフラグ
        /// </summary>
        public bool Exiting { get; private set; }

        /// <summary>
        /// メインのForm
        /// これがないとMessageBox.Show()などで親を指定できなくて困る。
        /// </summary>
        private Form mainForm { get; set; }

        /// <summary>
        /// UI threadで実行したい時にこれを用いる。
        ///
        /// 1) UIスレッドでのactionの実行完了を待つ。
        /// 2) UI Threadで発生した例外はUI Threadを抜けて伝播する。
        /// </summary>
        /// <param name="action"></param>
        public void UIThread(Action action)
        {
            if (mainForm != null && mainForm.IsHandleCreated && !mainForm.IsDisposed)
            {
                if (mainForm.InvokeRequired)
                {
                    Exception e = null;
                    try
                    {
                        mainForm.Invoke((Action)( ()=>
                        {
                            try
                            {
                                action();
                            } catch (Exception ex)
                            {
                                e = ex;
                            }
                        }));
                        // これで結果が返るまで待つはず..
                        // ここでウィンドウが破棄される可能性があるので成功するとは限らない。
                    }
                    catch { }

                    // Invoke()中に親Windowが解体されたなら例外は無視して良いが、
                    // さもなくば、このUI Thread内で発生した例外を伝播する必要がある。
                    if (e != null)
                        throw e;
                }
                else
                    action();
            }
            else
            {
                // メインウインドウ、解体後。
                action();
            }
        }

    }
}

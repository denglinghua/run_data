﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;

namespace RunData
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Text = this.Text + string.Format("({0})", Version.VER_NO);

            string fileFilter = "Excel文件|*.xls;*.xlsx";
            this.openFileDialogRunFile.Filter = fileFilter;
            this.openFileDialogRunDetail.Filter = fileFilter;
            this.openFileDialogNoRunFile.Filter = fileFilter;
            this.openFileDialogLeaveFile.Filter = fileFilter;

            this.saveFileDialogReport.DefaultExt = "xlsx";

            CheckDoButtonState();            

            Logger.Init(this.AppendNewLineResult);
        }

        private void buttonRunRecordFileSelect_Click(object sender, EventArgs e)
        {
            if (this.openFileDialogRunFile.ShowDialog() == DialogResult.OK)
            {
                this.textBoxRunRecordFile.Text = this.openFileDialogRunFile.FileName;
                CheckDoButtonState();
            }
        }

        private void buttonRunDetailFileSelect_Click(object sender, EventArgs e)
        {
            if (this.openFileDialogRunDetail.ShowDialog() == DialogResult.OK)
            {
                this.textBoxRunDetailFile.Text = this.openFileDialogRunDetail.FileName;
            }
        }

        private void buttonNoRunFilesSelect_Click(object sender, EventArgs e)
        {
            if (this.openFileDialogNoRunFile.ShowDialog() == DialogResult.OK)
            {
                this.textBoxNoRunFiles.Lines = this.openFileDialogNoRunFile.FileNames;
                CheckDoButtonState();
            }
        }

        private void buttonLeaveFileSelect_Click(object sender, EventArgs e)
        {
            if (this.openFileDialogLeaveFile.ShowDialog() == DialogResult.OK)
            {
                this.textBoxLeaveFile.Text = this.openFileDialogLeaveFile.FileName;
            }
        }

        private void buttonBatchSelect_Click(object sender, EventArgs e)
        {
            this.openFileDialogBatchSelect.Filter = "Excel|*.xlsx;*.xls|All|*.*";
            if (this.openFileDialogBatchSelect.ShowDialog() == DialogResult.OK)
            {
                string[] files = this.openFileDialogBatchSelect.FileNames;
                this.AutoFillFiles(files);
                CheckDoButtonState();
            }
        }

        private bool isReportSaved = false;


        private void buttonDo_Click(object sender, EventArgs e)
        {
            this.textBoxLog.Clear();

            this.isReportSaved = false;

            if (!CheckDataSourceFileOpen())
            {
                return;
            }

            Logger.Info(string.Format("开始运行（版本号：{0}）...", Version.VER_NO));

            try
            {
                this.Do();

                Logger.Info("运行结束。");

                if (!isReportSaved) return;

                if (MessageBox.Show("报表已经保存，要打开看看吗？", "打开", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    this.OpenReport(this.saveFileDialogReport.FileName);
                }
            }
            catch(RunDataException ex)
            {
                Logger.Info("运行出现错误:");
                Logger.Info(ex.Message);
            }
            catch (Exception ex)
            {
                Logger.Info("运行出现错误:");
                Logger.Info(ex.ToString());
            }
        }

        private void Do()
        {
            DataSource.Instance.LoadData(this.textBoxRunRecordFile.Text, this.textBoxRunDetailFile.Text, this.textBoxNoRunFiles.Lines, this.textBoxLeaveFile.Text);
            DataSource.Instance.HandleData();

            MessageBox.Show("报表已经生成，请选择保存文件夹。");

            this.saveFileDialogReport.FileName = string.Format("{0}-{1}-报表.xlsx", DataSource.Instance.CurrentDateRange, DataSource.Instance.Team);
            if (this.saveFileDialogReport.ShowDialog() == DialogResult.OK)
            {
                bool fileExists = File.Exists(this.saveFileDialogReport.FileName);
                if (fileExists && !CheckFileOpen(this.saveFileDialogReport.FileName))
                {
                    return;
                }
            }

            new Reporter().ToExcel(DataSource.Instance, this.saveFileDialogReport.FileName);
            this.isReportSaved = true;

            DataSource.Instance.Save();

            DataAnalysis.Facade.Analyze(DataSource.Instance);
        }

        private void OpenReport(string file)
        {
            Process.Start(file);
        }

        private void CheckDoButtonState()
        {
            if (this.textBoxRunRecordFile.Text != String.Empty && this.textBoxNoRunFiles.Text != string.Empty)
            {
                this.buttonDo.Enabled = true;
                this.buttonDo.BackColor = Color.LawnGreen;
            }
            else
            {
                this.buttonDo.Enabled = false;
            }
        }

        private bool CheckDataSourceFileOpen()
        {
            List<string> files = new List<string>();
            files.Add(this.textBoxRunRecordFile.Text);
            if (this.textBoxRunDetailFile.Text != string.Empty)
            {
                files.Add(this.textBoxRunDetailFile.Text);
            }
            files.AddRange(this.textBoxNoRunFiles.Lines);
            if (this.textBoxLeaveFile.Text != string.Empty)
            {
                files.Add(this.textBoxLeaveFile.Text);
            }

            foreach (string file in files)
            {
                if (!CheckFileOpen(file))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CheckFileOpen(string file)
        {
            try
            {
                using (FileStream FS = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                }
            }
            catch (IOException)
            {
                MessageBox.Show(string.Format("请先关闭文件{0}，再重试。", file), "关闭文件", MessageBoxButtons.OK);
                return false;
            }

            return true;
        }

        private void AppendNewLineResult(String text)
        {
            this.textBoxLog.Text += (text + Environment.NewLine);
        }

        private void textBoxLog_TextChanged(object sender, EventArgs e)
        {
            this.textBoxLog.SelectionStart = this.textBoxLog.Text.Length;
            this.textBoxLog.ScrollToCaret();
        }

        private void AutoFillFiles(string[] files)
        {
            var dict = new Dictionary<string, TextBox>
            {
                { "跑步数据统计", this.textBoxRunRecordFile },
                { "原始跑步记录", this.textBoxRunDetailFile },
                { "无跑步成员", this.textBoxNoRunFiles },
                { "请假", this.textBoxLeaveFile }
            };

            foreach (string keyword in dict.Keys)
            {
                TextBox tb = dict[keyword];
                tb.Clear();

                foreach (string file in files)
                {
                    if (file.Contains(keyword))
                    {
                        if (!string.IsNullOrEmpty(tb.Text)) tb.AppendText(Environment.NewLine);
                        tb.AppendText(file);
                        if (!tb.Multiline) break;
                    }
                }
            }
        }

        private void InitTestData()
        {
            this.textBoxNoRunFiles.Text = @"C:\Users\denglinghua\Desktop\run\data\_201901_广·马帮_天马分队_无跑步成员_0107-0113_14p7.xlsx";
            this.textBoxRunRecordFile.Text = @"C:\Users\denglinghua\Desktop\run\data\_201901_广·马帮跑步数据统计_0107-0113_gbv3.xlsx";
            this.textBoxLeaveFile.Text = @"C:\Users\denglinghua\Desktop\run\data\天马分队请假条报名清单.xls";
        }        
    }
}

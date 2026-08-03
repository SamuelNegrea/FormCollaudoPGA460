namespace FormCollaudoPGA460
{
    /// <summary>
    /// Defines the <see cref="Form1" />
    /// </summary>
    internal partial class Form1
    {
        /// <summary>
        /// Required designer variable
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor
        /// </summary>
        private void InitializeComponent()
        {
            txtIndirizzo = new System.Windows.Forms.TextBox();
            btnScrivi = new System.Windows.Forms.Button();
            btnLeggi = new System.Windows.Forms.Button();
            labelIndirizzo = new System.Windows.Forms.Label();
            cmbCom = new System.Windows.Forms.ComboBox();
            btnReadAll = new System.Windows.Forms.Button();
            btnWriteEEPROM = new System.Windows.Forms.Button();
            dgvRegisters = new System.Windows.Forms.DataGridView();
            txtDistance = new System.Windows.Forms.TextBox();
            txtWidth = new System.Windows.Forms.TextBox();
            txtAmplitude = new System.Windows.Forms.TextBox();
            panelTrigger = new System.Windows.Forms.Panel();
            labelDistanzaRilevata = new System.Windows.Forms.Label();
            labelDimensioneOggetto = new System.Windows.Forms.Label();
            labelIntensitàOggetto = new System.Windows.Forms.Label();
            labelOggettoRilevato = new System.Windows.Forms.Label();
            labelPorta = new System.Windows.Forms.Label();
            pictureBoxDump = new System.Windows.Forms.PictureBox();
            btnReadConfig = new System.Windows.Forms.Button();
            chkObjectDetect = new System.Windows.Forms.CheckBox();
            numDistanceSet = new System.Windows.Forms.NumericUpDown();
            numWidthSet = new System.Windows.Forms.NumericUpDown();
            numAmplitudeSet = new System.Windows.Forms.NumericUpDown();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            btnScanMisure = new System.Windows.Forms.Button();
            btnScanGrafico = new System.Windows.Forms.Button();
            btnSaveConfigFile = new System.Windows.Forms.Button();
            btnLoadConfigFile = new System.Windows.Forms.Button();
            label4 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            label6 = new System.Windows.Forms.Label();
            btnWriteThreshold = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)dgvRegisters).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxDump).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numDistanceSet).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numWidthSet).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numAmplitudeSet).BeginInit();
            SuspendLayout();
            // 
            // txtIndirizzo
            // 
            txtIndirizzo.Location = new System.Drawing.Point(161, 91);
            txtIndirizzo.Name = "txtIndirizzo";
            txtIndirizzo.Size = new System.Drawing.Size(147, 23);
            txtIndirizzo.TabIndex = 3;
            // 
            // btnScrivi
            // 
            btnScrivi.Location = new System.Drawing.Point(124, 135);
            btnScrivi.Name = "btnScrivi";
            btnScrivi.Size = new System.Drawing.Size(116, 48);
            btnScrivi.TabIndex = 1;
            btnScrivi.Text = "Trasmetti a PGA";
            btnScrivi.UseVisualStyleBackColor = true;
            btnScrivi.Click += btnScrivi_Click;
            // 
            // btnLeggi
            // 
            btnLeggi.Location = new System.Drawing.Point(258, 135);
            btnLeggi.Name = "btnLeggi";
            btnLeggi.Size = new System.Drawing.Size(114, 48);
            btnLeggi.TabIndex = 0;
            btnLeggi.Text = "Leggi da PGA";
            btnLeggi.UseVisualStyleBackColor = true;
            btnLeggi.Click += btnLeggi_Click;
            // 
            // labelIndirizzo
            // 
            labelIndirizzo.AutoSize = true;
            labelIndirizzo.Location = new System.Drawing.Point(83, 91);
            labelIndirizzo.Name = "labelIndirizzo";
            labelIndirizzo.Size = new System.Drawing.Size(51, 15);
            labelIndirizzo.TabIndex = 4;
            labelIndirizzo.Text = "indirizzo";
            // 
            // cmbCom
            // 
            cmbCom.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbCom.FormattingEnabled = true;
            cmbCom.Location = new System.Drawing.Point(187, 49);
            cmbCom.Name = "cmbCom";
            cmbCom.Size = new System.Drawing.Size(121, 23);
            cmbCom.TabIndex = 6;
            // 
            // btnReadAll
            // 
            btnReadAll.Location = new System.Drawing.Point(124, 212);
            btnReadAll.Name = "btnReadAll";
            btnReadAll.Size = new System.Drawing.Size(164, 32);
            btnReadAll.TabIndex = 8;
            btnReadAll.Text = "Campi --> Registri";
            btnReadAll.UseVisualStyleBackColor = true;
            btnReadAll.Click += btnReadAll_Click;
            // 
            // btnWriteEEPROM
            // 
            btnWriteEEPROM.Location = new System.Drawing.Point(319, 212);
            btnWriteEEPROM.Name = "btnWriteEEPROM";
            btnWriteEEPROM.Size = new System.Drawing.Size(166, 32);
            btnWriteEEPROM.TabIndex = 9;
            btnWriteEEPROM.Text = "scrivi EEPROM";
            btnWriteEEPROM.UseVisualStyleBackColor = true;
            // 
            // dgvRegisters
            // 
            dgvRegisters.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvRegisters.Location = new System.Drawing.Point(599, 319);
            dgvRegisters.Name = "dgvRegisters";
            dgvRegisters.Size = new System.Drawing.Size(729, 213);
            dgvRegisters.TabIndex = 11;
            dgvRegisters.CellContentClick += dataGridView1_CellContentClick;
            // 
            // txtDistance
            // 
            txtDistance.Location = new System.Drawing.Point(145, 643);
            txtDistance.Name = "txtDistance";
            txtDistance.Size = new System.Drawing.Size(100, 23);
            txtDistance.TabIndex = 12;
            // 
            // txtWidth
            // 
            txtWidth.Location = new System.Drawing.Point(145, 694);
            txtWidth.Name = "txtWidth";
            txtWidth.Size = new System.Drawing.Size(100, 23);
            txtWidth.TabIndex = 13;
            // 
            // txtAmplitude
            // 
            txtAmplitude.Location = new System.Drawing.Point(145, 748);
            txtAmplitude.Name = "txtAmplitude";
            txtAmplitude.Size = new System.Drawing.Size(100, 23);
            txtAmplitude.TabIndex = 14;
            // 
            // panelTrigger
            // 
            panelTrigger.Location = new System.Drawing.Point(436, 677);
            panelTrigger.Name = "panelTrigger";
            panelTrigger.Size = new System.Drawing.Size(40, 40);
            panelTrigger.TabIndex = 15;
            // 
            // labelDistanzaRilevata
            // 
            labelDistanzaRilevata.AutoSize = true;
            labelDistanzaRilevata.Location = new System.Drawing.Point(12, 646);
            labelDistanzaRilevata.Name = "labelDistanzaRilevata";
            labelDistanzaRilevata.Size = new System.Drawing.Size(92, 15);
            labelDistanzaRilevata.TabIndex = 16;
            labelDistanzaRilevata.Text = "Distanza rilevata";
            // 
            // labelDimensioneOggetto
            // 
            labelDimensioneOggetto.AutoSize = true;
            labelDimensioneOggetto.Location = new System.Drawing.Point(1, 697);
            labelDimensioneOggetto.Name = "labelDimensioneOggetto";
            labelDimensioneOggetto.Size = new System.Drawing.Size(115, 15);
            labelDimensioneOggetto.TabIndex = 17;
            labelDimensioneOggetto.Text = "Dimensione oggetto";
            // 
            // labelIntensitàOggetto
            // 
            labelIntensitàOggetto.AutoSize = true;
            labelIntensitàOggetto.Location = new System.Drawing.Point(11, 751);
            labelIntensitàOggetto.Name = "labelIntensitàOggetto";
            labelIntensitàOggetto.Size = new System.Drawing.Size(97, 15);
            labelIntensitàOggetto.TabIndex = 18;
            labelIntensitàOggetto.Text = "Intensità oggetto";
            // 
            // labelOggettoRilevato
            // 
            labelOggettoRilevato.AutoSize = true;
            labelOggettoRilevato.Location = new System.Drawing.Point(308, 694);
            labelOggettoRilevato.Name = "labelOggettoRilevato";
            labelOggettoRilevato.Size = new System.Drawing.Size(93, 15);
            labelOggettoRilevato.TabIndex = 19;
            labelOggettoRilevato.Text = "Oggetto rilevato";
            // 
            // labelPorta
            // 
            labelPorta.AutoSize = true;
            labelPorta.Location = new System.Drawing.Point(83, 49);
            labelPorta.Name = "labelPorta";
            labelPorta.Size = new System.Drawing.Size(66, 15);
            labelPorta.TabIndex = 20;
            labelPorta.Text = "Porta COM";
            // 
            // pictureBoxDump
            // 
            pictureBoxDump.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            pictureBoxDump.Location = new System.Drawing.Point(599, 555);
            pictureBoxDump.Name = "pictureBoxDump";
            pictureBoxDump.Size = new System.Drawing.Size(721, 281);
            pictureBoxDump.TabIndex = 21;
            pictureBoxDump.TabStop = false;
            // 
            // btnReadConfig
            // 
            btnReadConfig.Location = new System.Drawing.Point(120, 266);
            btnReadConfig.Name = "btnReadConfig";
            btnReadConfig.Size = new System.Drawing.Size(168, 41);
            btnReadConfig.TabIndex = 22;
            btnReadConfig.Text = "Registri --> Campi";
            btnReadConfig.UseVisualStyleBackColor = true;
            // 
            // chkObjectDetect
            // 
            chkObjectDetect.AutoSize = true;
            chkObjectDetect.Location = new System.Drawing.Point(11, 450);
            chkObjectDetect.Name = "chkObjectDetect";
            chkObjectDetect.Size = new System.Drawing.Size(171, 19);
            chkObjectDetect.TabIndex = 24;
            chkObjectDetect.Text = "Abilita rilevamento oggetto";
            chkObjectDetect.UseVisualStyleBackColor = true;
            // 
            // numDistanceSet
            // 
            numDistanceSet.Location = new System.Drawing.Point(168, 488);
            numDistanceSet.Name = "numDistanceSet";
            numDistanceSet.Size = new System.Drawing.Size(120, 23);
            numDistanceSet.TabIndex = 26;
            // 
            // numWidthSet
            // 
            numWidthSet.Location = new System.Drawing.Point(168, 526);
            numWidthSet.Name = "numWidthSet";
            numWidthSet.Size = new System.Drawing.Size(120, 23);
            numWidthSet.TabIndex = 27;
            // 
            // numAmplitudeSet
            // 
            numAmplitudeSet.Location = new System.Drawing.Point(168, 573);
            numAmplitudeSet.Name = "numAmplitudeSet";
            numAmplitudeSet.Size = new System.Drawing.Size(120, 23);
            numAmplitudeSet.TabIndex = 28;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(6, 490);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(129, 15);
            label1.TabIndex = 29;
            label1.Text = "Distanza massima [cm]";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(11, 534);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(114, 15);
            label2.TabIndex = 30;
            label2.Text = "Dimensione minima";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(14, 575);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(96, 15);
            label3.TabIndex = 31;
            label3.Text = "Intensità minima";
            // 
            // btnScanMisure
            // 
            btnScanMisure.Location = new System.Drawing.Point(29, 403);
            btnScanMisure.Name = "btnScanMisure";
            btnScanMisure.Size = new System.Drawing.Size(133, 23);
            btnScanMisure.TabIndex = 2;
            btnScanMisure.Text = "Attiva Scan Misure";
            btnScanMisure.UseVisualStyleBackColor = true;
            btnScanMisure.Visible = false;
            // 
            // btnScanGrafico
            // 
            btnScanGrafico.Location = new System.Drawing.Point(356, 403);
            btnScanGrafico.Name = "btnScanGrafico";
            btnScanGrafico.Size = new System.Drawing.Size(120, 32);
            btnScanGrafico.TabIndex = 33;
            btnScanGrafico.Text = "Attiva Scan";
            btnScanGrafico.UseVisualStyleBackColor = true;
            // 
            // btnSaveConfigFile
            // 
            btnSaveConfigFile.Location = new System.Drawing.Point(319, 751);
            btnSaveConfigFile.Name = "btnSaveConfigFile";
            btnSaveConfigFile.Size = new System.Drawing.Size(150, 31);
            btnSaveConfigFile.TabIndex = 34;
            btnSaveConfigFile.Text = "Salva su file";
            btnSaveConfigFile.UseVisualStyleBackColor = true;
            // 
            // btnLoadConfigFile
            // 
            btnLoadConfigFile.Location = new System.Drawing.Point(319, 803);
            btnLoadConfigFile.Name = "btnLoadConfigFile";
            btnLoadConfigFile.Size = new System.Drawing.Size(150, 33);
            btnLoadConfigFile.TabIndex = 35;
            btnLoadConfigFile.Text = "Carica da file";
            btnLoadConfigFile.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(13, 6);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(322, 15);
            label4.TabIndex = 36;
            label4.Text = "PROGEL ENGINEERING S.R.L.   PGA460 Configurator. Rev 2.0";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(599, 301);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(46, 15);
            label5.TabIndex = 37;
            label5.Text = "Registri";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(601, 9);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(42, 15);
            label6.TabIndex = 38;
            label6.Text = "Campi";
            // 
            // btnWriteThreshold
            // 
            btnWriteThreshold.Location = new System.Drawing.Point(319, 266);
            btnWriteThreshold.Name = "btnWriteThreshold";
            btnWriteThreshold.Size = new System.Drawing.Size(166, 41);
            btnWriteThreshold.TabIndex = 39;
            btnWriteThreshold.Text = "Scrivi Threshold";
            btnWriteThreshold.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            ClientSize = new System.Drawing.Size(1384, 861);
            Controls.Add(btnWriteThreshold);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(btnLoadConfigFile);
            Controls.Add(btnSaveConfigFile);
            Controls.Add(btnScanGrafico);
            Controls.Add(btnScanMisure);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(numAmplitudeSet);
            Controls.Add(numWidthSet);
            Controls.Add(numDistanceSet);
            Controls.Add(chkObjectDetect);
            Controls.Add(btnReadConfig);
            Controls.Add(pictureBoxDump);
            Controls.Add(labelPorta);
            Controls.Add(labelOggettoRilevato);
            Controls.Add(labelIntensitàOggetto);
            Controls.Add(labelDimensioneOggetto);
            Controls.Add(labelDistanzaRilevata);
            Controls.Add(panelTrigger);
            Controls.Add(txtAmplitude);
            Controls.Add(txtWidth);
            Controls.Add(txtDistance);
            Controls.Add(dgvRegisters);
            Controls.Add(btnWriteEEPROM);
            Controls.Add(btnReadAll);
            Controls.Add(cmbCom);
            Controls.Add(labelIndirizzo);
            Controls.Add(btnLeggi);
            Controls.Add(btnScrivi);
            Controls.Add(txtIndirizzo);
            Name = "Form1";
            Text = "PGA460 Tool";
            ((System.ComponentModel.ISupportInitialize)dgvRegisters).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxDump).EndInit();
            ((System.ComponentModel.ISupportInitialize)numDistanceSet).EndInit();
            ((System.ComponentModel.ISupportInitialize)numWidthSet).EndInit();
            ((System.ComponentModel.ISupportInitialize)numAmplitudeSet).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        /// <summary>
        /// Defines the txtIndirizzo
        /// </summary>
        private System.Windows.Forms.TextBox txtIndirizzo;

        /// <summary>
        /// Defines the btnScrivi
        /// </summary>
        private System.Windows.Forms.Button btnScrivi;

        /// <summary>
        /// Defines the btnLeggi
        /// </summary>
        private System.Windows.Forms.Button btnLeggi;

        /// <summary>
        /// Defines the labelIndirizzo
        /// </summary>
        private System.Windows.Forms.Label labelIndirizzo;

        /// <summary>
        /// Defines the cmbCom
        /// </summary>
        private System.Windows.Forms.ComboBox cmbCom;

        /// <summary>
        /// Defines the btnReadAll
        /// </summary>
        private System.Windows.Forms.Button btnReadAll;

        /// <summary>
        /// Defines the btnReadEEPROM
        /// </summary>
        private System.Windows.Forms.Button btnWriteEEPROM;

        /// <summary>
        /// Defines the dgvRegisters
        /// </summary>
        private System.Windows.Forms.DataGridView dgvRegisters;

        /// <summary>
        /// Defines the txtDistance
        /// </summary>
        private System.Windows.Forms.TextBox txtDistance;

        /// <summary>
        /// Defines the txtWidth
        /// </summary>
        private System.Windows.Forms.TextBox txtWidth;

        /// <summary>
        /// Defines the txtAmplitude
        /// </summary>
        private System.Windows.Forms.TextBox txtAmplitude;

        /// <summary>
        /// Defines the panelTrigger
        /// </summary>
        private System.Windows.Forms.Panel panelTrigger;

        /// <summary>
        /// Defines the labelDistanzaRilevata
        /// </summary>
        private System.Windows.Forms.Label labelDistanzaRilevata;

        /// <summary>
        /// Defines the labelDimensioneOggetto
        /// </summary>
        private System.Windows.Forms.Label labelDimensioneOggetto;

        /// <summary>
        /// Defines the labelIntensitàOggetto
        /// </summary>
        private System.Windows.Forms.Label labelIntensitàOggetto;

        /// <summary>
        /// Defines the labelOggettoRilevato
        /// </summary>
        private System.Windows.Forms.Label labelOggettoRilevato;

        /// <summary>
        /// Defines the labelPorta
        /// </summary>
        private System.Windows.Forms.Label labelPorta;
        private System.Windows.Forms.PictureBox pictureBoxDump;
        private System.Windows.Forms.Button btnReadConfig;
        private System.Windows.Forms.CheckBox chkObjectDetect;
        private System.Windows.Forms.NumericUpDown numDistanceSet;
        private System.Windows.Forms.NumericUpDown numWidthSet;
        private System.Windows.Forms.NumericUpDown numAmplitudeSet;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnScanMisure;
        private System.Windows.Forms.Button btnScanGrafico;
        private System.Windows.Forms.Button btnSaveConfigFile;
        private System.Windows.Forms.Button btnLoadConfigFile;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button btnWriteThreshold;
    }
}

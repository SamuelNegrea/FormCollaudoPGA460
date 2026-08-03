using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

namespace FormCollaudoPGA460
{
    partial class Form1 : Form
    {
        SerialPort serial;
        private System.Windows.Forms.Timer scanTimer = new();
        private bool scanEnabled = false;
        private bool scanEnabled_prec = false;
        private bool misureScanEnabled = false;
        private bool misureScanEnabled_prec = false;

        // --- Pulsante unico "Attiva Scan": alterna ciclicamente Grafico <-> Misure ---
        private enum ScanPhase { Grafico, Misure }
        private bool combinedScanEnabled = false;
        private ScanPhase currentScanPhase = ScanPhase.Grafico;
        private const int SCAN_PHASE_INTERVAL_MS = 300;
        private volatile bool graficoRequested = false;
        private byte[] virtualRegs = new byte[256];
        private bool offlineMode = false;
        private DataGridView dgvFields;
        private readonly object serialLock = new object();
        private volatile bool refreshGraph = true;
        private List<PointF> cachedThresholdPts = null;
        private List<PointF> cachedGainPts = null;
        private byte[] lastEchoDump = new byte[128];
        private bool forceRegisterWrite = false;
        private byte currentUartAddress = 0;
        private bool eepromWriteInProgress = false;

        private void LoadDefaultRegisters()
        {
            WriteRegister(0x14, 0xA5);
            WriteRegister(0x15, 0x69);
            WriteRegister(0x16, 0xAA);
            WriteRegister(0x17, 0x79);
            WriteRegister(0x18, 0xE7);
            WriteRegister(0x19, 0x9E);
            WriteRegister(0x1A, 0x04);
            WriteRegister(0x1B, 0x40);
            WriteRegister(0x1C, 0x5A);
            WriteRegister(0x1D, 0x05);
            WriteRegister(0x1E, 0x08);
            WriteRegister(0x1F, 0x12);
            WriteRegister(0x20, 0x11);
            WriteRegister(0x21, 0x7F);
            WriteRegister(0x22, 0x1C);
            WriteRegister(0x23, 0x00);
            WriteRegister(0x24, 0xEE);
            WriteRegister(0x25, 0x7C);
            WriteRegister(0x26, 0x0A);

            WriteRegister(0x29, 0x09);

            WriteRegister(0x5F, 0xAF);
            WriteRegister(0x60, 0xC8);
            WriteRegister(0x61, 0x88);
            WriteRegister(0x62, 0x88);
            WriteRegister(0x63, 0x88);
            WriteRegister(0x64, 0x88);

            WriteRegister(0x65, 0x84);
            WriteRegister(0x66, 0x21);
            WriteRegister(0x67, 0x08);
            WriteRegister(0x68, 0x42);
            WriteRegister(0x69, 0x10);

            WriteRegister(0x6A, 0x80);
            WriteRegister(0x6B, 0x80);
            WriteRegister(0x6C, 0x80);
            WriteRegister(0x6D, 0x80);
            WriteRegister(0x6E, 0x00);

            WriteRegister(0x6F, 0x00);
            WriteRegister(0x70, 0x00);
            WriteRegister(0x71, 0x00);
            WriteRegister(0x72, 0x00);
            WriteRegister(0x73, 0x00);
            WriteRegister(0x74, 0x00);
            WriteRegister(0x75, 0x00);
            WriteRegister(0x76, 0x00);
            WriteRegister(0x77, 0x00);
            WriteRegister(0x78, 0x00);
            WriteRegister(0x79, 0x00);
            WriteRegister(0x7A, 0x00);
            WriteRegister(0x7B, 0x00);
            WriteRegister(0x7C, 0x00);
            WriteRegister(0x7D, 0x00);
            WriteRegister(0x7E, 0x00);
        }

        private class PGA460Fields
        {
            public byte TVG_T0, TVG_T1;
            public byte TVG_T2, TVG_T3;
            public byte TVG_T4, TVG_T5;

            public byte TVG_G1, TVG_G2;
            public byte TVG_G3, TVG_G4;
            public byte TVG_G5, FREQ_SHIFT;

            public byte BPF_BW, GAIN_INIT;

            public byte FREQ;

            public byte THR_CMP_DEGLTCH, PULSE_DT;

            public byte IO_IF_SEL, UART_DIAG, IO_DIS, P1_PULSE;

            public byte UART_ADDR, P2_PULSE;

            public byte DIS_CL, CURR_LIM1;

            public byte LPF_CO, CURR_LIM2;

            public byte P1_REC, P2_REC;

            public byte FDIAG_LEN, FDIAG_START;

            public byte FDIAG_ERR_TH, SAT_TH, P1_NLS_EN;

            public byte P2_NLS_EN, VPWR_OV_TH, LPM_TMR, FVOLT_ERR_TH;

            public byte AFE_GAIN_RNG, LPM_EN, DECPL_TEMP_SEL, DECPL_T;

            public byte NOISE_LVL, SCALE_K, SCALE_N;

            public byte TEMP_GAIN, TEMP_OFF;

            public byte P1_DIG_GAIN_LR_ST, P1_DIG_GAIN_LR, P1_DIG_GAIN_SR;

            public byte P2_DIG_GAIN_LR_ST, P2_DIG_GAIN_LR, P2_DIG_GAIN_SR;

            public byte EE_CRC;

            public byte BPF_A2_MSB;
            public byte BPF_A2_LSB;

            public byte BPF_A3_MSB;
            public byte BPF_A3_LSB;

            public byte BPF_B1_MSB;
            public byte BPF_B1_LSB;

            public byte LPF_A2_MSB;
            public byte LPF_A2_LSB;

            public byte LPF_B1_MSB;
            public byte LPF_B1_LSB;

            public byte DATADUMP_EN, EE_UNLCK, EE_PRGM_OK, EE_RLOAD, EE_PRGM;

            public byte TEST_MUX, SAMPLE_SEL, DP_MUX;

            public byte REV_ID, OPT_ID, CMW_WU_ERR, THR_CRC_ERR, EE_CRC_ERR, TRIM_CRC_ERR;

            public byte RESERVED, TSD_PROT, IOREG_OV, IOREG_UV, AVDD_OV, AVDD_UV, VPWR_OV, VPWR_UV;

            public byte TH_P1_T1, TH_P1_T2;

            public byte TH_P1_T3, TH_P1_T4;

            public byte TH_P1_T5, TH_P1_T6;

            public byte TH_P1_T7, TH_P1_T8;

            public byte TH_P1_T9, TH_P1_T10;

            public byte TH_P1_T11, TH_P1_T12;

            public byte TH_P1_L1, TH_P1_L2;

            public byte TH_P1_L3, TH_P1_L4;

            public byte TH_P1_L5;

            public byte TH_P1_L6, TH_P1_L7;

            public byte TH_P1_L8;

            public byte TH_P1_L9;

            public byte TH_P1_L10;

            public byte TH_P1_L11;

            public byte TH_P1_L12;

            public byte TH_P1_OFF;

        }

        private PGA460Fields fields = new PGA460Fields();


        private void DecodeRegisters14To6E()
        { 
            DecodeTVGain();
            DecodeTVGGainValues();
            DecodeMainConfig();
            DecodeEepromControl();
            DecodeFilterConfig();
            DecodeDeviceStatus();
            DecodeP1Threshold();
        }

        private void EncodeRegisters14To6E(bool writeEepromControl = false)
        {
            EncodeTVGain();
            EncodeTVGGainValues();
            EncodeMainConfig();

            if (writeEepromControl)
                EncodeEepromControl();

            EncodeFilterConfig();
            EncodeP1Threshold();
        }

        private void DecodeTVGain()
        {
            byte reg14 = ReadRegister(0x14);
            byte reg15 = ReadRegister(0x15);
            byte reg16 = ReadRegister(0x16);

            fields.TVG_T0 = (byte)((reg14 >> 4) & 0x0F);
            fields.TVG_T1 = (byte)(reg14 & 0x0F);
            fields.TVG_T2 = (byte)((reg15 >> 4) & 0x0F);
            fields.TVG_T3 = (byte)(reg15 & 0x0F);
            fields.TVG_T4 = (byte)((reg16 >> 4) & 0x0F);
            fields.TVG_T5 = (byte)(reg16 & 0x0F);
        }

        private void EncodeTVGain()
        {
            WriteRegister(0x14, (byte)(((fields.TVG_T0 & 0x0F) << 4) | (fields.TVG_T1 & 0x0F)));
            WriteRegister(0x15, (byte)(((fields.TVG_T2 & 0x0F) << 4) | (fields.TVG_T3 & 0x0F)));
            WriteRegister(0x16, (byte)(((fields.TVG_T4 & 0x0F) << 4) | (fields.TVG_T5 & 0x0F)));
        }

        private void DecodeTVGGainValues()
        {
            byte reg17 = ReadRegister(0x17);
            byte reg18 = ReadRegister(0x18);
            byte reg19 = ReadRegister(0x19);
            byte reg1A = ReadRegister(0x1A);

            fields.TVG_G1 = (byte)((reg17 >> 2) & 0x3F);
            fields.TVG_G2 = (byte)(((reg17 & 0x03) << 4) | ((reg18 >> 4) & 0x0F));
            fields.TVG_G3 = (byte)(((reg18 & 0x0F) << 2) | ((reg19 >> 6) & 0x03));
            fields.TVG_G4 = (byte)(reg19 & 0x3F);
            fields.TVG_G5 = (byte)((reg1A >> 2) & 0x3F);
            fields.FREQ_SHIFT = (byte)(reg1A & 0x01);
        }

        private void EncodeTVGGainValues()
        {
            WriteRegister(0x17, (byte)(((fields.TVG_G1 & 0x3F) << 2) | ((fields.TVG_G2 >> 4) & 0x03)));
            WriteRegister(0x18, (byte)(((fields.TVG_G2 & 0x0F) << 4) | ((fields.TVG_G3 >> 2) & 0x0F)));
            WriteRegister(0x19, (byte)(((fields.TVG_G3 & 0x03) << 6) | (fields.TVG_G4 & 0x3F)));

            byte reg1A = (byte)(
                (ReadRegister(0x1A) & 0x02) |
                ((fields.TVG_G5 & 0x3F) << 2) |
                (fields.FREQ_SHIFT & 0x01));

            WriteRegister(0x1A, reg1A);
        }

        private void DecodeMainConfig()
        {
            byte reg1B = ReadRegister(0x1B);
            byte reg1D = ReadRegister(0x1D);
            byte reg1E = ReadRegister(0x1E);
            byte reg1F = ReadRegister(0x1F);
            byte reg20 = ReadRegister(0x20);
            byte reg21 = ReadRegister(0x21);
            byte reg22 = ReadRegister(0x22);
            byte reg23 = ReadRegister(0x23);
            byte reg24 = ReadRegister(0x24);
            byte reg25 = ReadRegister(0x25);
            byte reg26 = ReadRegister(0x26);
            byte reg27 = ReadRegister(0x27);
            byte reg28 = ReadRegister(0x28);
            byte reg29 = ReadRegister(0x29);
            byte reg2A = ReadRegister(0x2A);

            fields.BPF_BW = (byte)((reg1B >> 6) & 0x03);
            fields.GAIN_INIT = (byte)(reg1B & 0x3F);
            fields.FREQ = ReadRegister(0x1C);

            fields.THR_CMP_DEGLTCH = (byte)((reg1D >> 4) & 0x0F);
            fields.PULSE_DT = (byte)(reg1D & 0x0F);

            fields.IO_IF_SEL = (byte)((reg1E >> 7) & 0x01);
            fields.UART_DIAG = (byte)((reg1E >> 6) & 0x01);
            fields.IO_DIS = (byte)((reg1E >> 5) & 0x01);
            fields.P1_PULSE = (byte)(reg1E & 0x1F);

            fields.UART_ADDR = (byte)((reg1F >> 5) & 0x07);
            fields.P2_PULSE = (byte)(reg1F & 0x1F);

            fields.DIS_CL = (byte)((reg20 >> 7) & 0x01);
            fields.CURR_LIM1 = (byte)(reg20 & 0x3F);

            fields.LPF_CO = (byte)((reg21 >> 6) & 0x03);
            fields.CURR_LIM2 = (byte)(reg21 & 0x3F);

            fields.P1_REC = (byte)((reg22 >> 4) & 0x0F);
            fields.P2_REC = (byte)(reg22 & 0x0F);

            fields.FDIAG_LEN = (byte)((reg23 >> 4) & 0x0F);
            fields.FDIAG_START = (byte)(reg23 & 0x0F);

            fields.FDIAG_ERR_TH = (byte)((reg24 >> 5) & 0x07);
            fields.SAT_TH = (byte)((reg24 >> 1) & 0x0F);
            fields.P1_NLS_EN = (byte)(reg24 & 0x01);

            fields.P2_NLS_EN = (byte)((reg25 >> 7) & 0x01);
            fields.VPWR_OV_TH = (byte)((reg25 >> 5) & 0x03);
            fields.LPM_TMR = (byte)((reg25 >> 3) & 0x03);
            fields.FVOLT_ERR_TH = (byte)(reg25 & 0x07);

            fields.AFE_GAIN_RNG = (byte)((reg26 >> 6) & 0x03);
            fields.LPM_EN = (byte)((reg26 >> 5) & 0x01);
            fields.DECPL_TEMP_SEL = (byte)((reg26 >> 4) & 0x01);
            fields.DECPL_T = (byte)(reg26 & 0x0F);

            fields.NOISE_LVL = (byte)((reg27 >> 3) & 0x1F);
            fields.SCALE_K = (byte)((reg27 >> 2) & 0x01);
            fields.SCALE_N = (byte)(reg27 & 0x03);

            fields.TEMP_GAIN = (byte)((reg28 >> 4) & 0x0F);
            fields.TEMP_OFF = (byte)(reg28 & 0x0F);

            fields.P1_DIG_GAIN_LR_ST = (byte)((reg29 >> 6) & 0x03);
            fields.P1_DIG_GAIN_LR = (byte)((reg29 >> 3) & 0x07);
            fields.P1_DIG_GAIN_SR = (byte)(reg29 & 0x07);

            fields.P2_DIG_GAIN_LR_ST = (byte)((reg2A >> 6) & 0x03);
            fields.P2_DIG_GAIN_LR = (byte)((reg2A >> 3) & 0x07);
            fields.P2_DIG_GAIN_SR = (byte)(reg2A & 0x07);

            fields.EE_CRC = ReadRegister(0x2B);
        }

        private void EncodeMainConfig()
        {
            WriteRegister(0x1B, (byte)(((fields.BPF_BW & 0x03) << 6) | (fields.GAIN_INIT & 0x3F)));
            WriteRegister(0x1C, fields.FREQ);
            WriteRegister(0x1D, (byte)(((fields.THR_CMP_DEGLTCH & 0x0F) << 4) | (fields.PULSE_DT & 0x0F)));
            WriteRegister(0x1E, (byte)(((fields.IO_IF_SEL & 0x01) << 7) | ((fields.UART_DIAG & 0x01) << 6) | ((fields.IO_DIS & 0x01) << 5) | (fields.P1_PULSE & 0x1F)));
            WriteRegister(0x1F, (byte)(((fields.UART_ADDR & 0x07) << 5) | (fields.P2_PULSE & 0x1F)));

            byte reg20 = (byte)((virtualRegs[0x20] & 0x40) | ((fields.DIS_CL & 0x01) << 7) | (fields.CURR_LIM1 & 0x3F));
            WriteRegister(0x20, reg20);

            WriteRegister(0x21, (byte)(((fields.LPF_CO & 0x03) << 6) | (fields.CURR_LIM2 & 0x3F)));
            WriteRegister(0x22, (byte)(((fields.P1_REC & 0x0F) << 4) | (fields.P2_REC & 0x0F)));
            WriteRegister(0x23, (byte)(((fields.FDIAG_LEN & 0x0F) << 4) | (fields.FDIAG_START & 0x0F)));
            WriteRegister(0x24, (byte)(((fields.FDIAG_ERR_TH & 0x07) << 5) | ((fields.SAT_TH & 0x0F) << 1) | (fields.P1_NLS_EN & 0x01)));
            WriteRegister(0x25, (byte)(((fields.P2_NLS_EN & 0x01) << 7) | ((fields.VPWR_OV_TH & 0x03) << 5) | ((fields.LPM_TMR & 0x03) << 3) | (fields.FVOLT_ERR_TH & 0x07)));
            WriteRegister(0x26, (byte)(((fields.AFE_GAIN_RNG & 0x03) << 6) | ((fields.LPM_EN & 0x01) << 5) | ((fields.DECPL_TEMP_SEL & 0x01) << 4) | (fields.DECPL_T & 0x0F)));
            WriteRegister(0x27, (byte)(((fields.NOISE_LVL & 0x1F) << 3) | ((fields.SCALE_K & 0x01) << 2) | (fields.SCALE_N & 0x03)));
            WriteRegister(0x28, (byte)(((fields.TEMP_GAIN & 0x0F) << 4) | (fields.TEMP_OFF & 0x0F)));
            WriteRegister(0x29, (byte)(((fields.P1_DIG_GAIN_LR_ST & 0x03) << 6) | ((fields.P1_DIG_GAIN_LR & 0x07) << 3) | (fields.P1_DIG_GAIN_SR & 0x07)));
            WriteRegister(0x2A, (byte)(((fields.P2_DIG_GAIN_LR_ST & 0x03) << 6) | ((fields.P2_DIG_GAIN_LR & 0x07) << 3) | (fields.P2_DIG_GAIN_SR & 0x07)));
        }

        private void DecodeEepromControl()
        {
            byte reg40 = ReadRegister(0x40);

            fields.DATADUMP_EN = (byte)((reg40 >> 7) & 0x01);
            fields.EE_UNLCK = (byte)((reg40 >> 3) & 0x0F);
            fields.EE_PRGM_OK = (byte)((reg40 >> 2) & 0x01);
            fields.EE_RLOAD = (byte)((reg40 >> 1) & 0x01);
            fields.EE_PRGM = (byte)(reg40 & 0x01);
        }

        private void EncodeEepromControl()
        {
            byte reg40 = (byte)(
                (virtualRegs[0x40] & 0x04) |
                ((fields.DATADUMP_EN & 0x01) << 7) |
                ((fields.EE_UNLCK & 0x0F) << 3) |
                ((fields.EE_RLOAD & 0x01) << 1) |
                (fields.EE_PRGM & 0x01));

            WriteRegister(0x40, reg40);
        }

        private void DecodeFilterConfig()
        {
            fields.BPF_A2_MSB = ReadRegister(0x41);
            fields.BPF_A2_LSB = ReadRegister(0x42);
            fields.BPF_A3_MSB = ReadRegister(0x43);
            fields.BPF_A3_LSB = ReadRegister(0x44);
            fields.BPF_B1_MSB = ReadRegister(0x45);
            fields.BPF_B1_LSB = ReadRegister(0x46);

            fields.LPF_A2_MSB = (byte)(ReadRegister(0x47) & 0x7F);
            fields.LPF_A2_LSB = ReadRegister(0x48);
            fields.LPF_B1_MSB = (byte)(ReadRegister(0x49) & 0x7F);
            fields.LPF_B1_LSB = ReadRegister(0x4A);

            byte reg4B = ReadRegister(0x4B);
            fields.TEST_MUX = (byte)((reg4B >> 5) & 0x07);
            fields.SAMPLE_SEL = (byte)((reg4B >> 3) & 0x01);
            fields.DP_MUX = (byte)(reg4B & 0x07);
        }

        private void EncodeFilterConfig()
        {
            WriteRegister(0x41, fields.BPF_A2_MSB);
            WriteRegister(0x42, fields.BPF_A2_LSB);
            WriteRegister(0x43, fields.BPF_A3_MSB);
            WriteRegister(0x44, fields.BPF_A3_LSB);
            WriteRegister(0x45, fields.BPF_B1_MSB);
            WriteRegister(0x46, fields.BPF_B1_LSB);
            WriteRegister(0x47, (byte)(((virtualRegs[0x47]) & 0x80) | (fields.LPF_A2_MSB & 0x7F)));
            WriteRegister(0x48, fields.LPF_A2_LSB);
            WriteRegister(0x49, (byte)((virtualRegs[0x49] & 0x80) | (fields.LPF_B1_MSB & 0x7F)));
            WriteRegister(0x4A, fields.LPF_B1_LSB);

            byte reg4B = (byte)(
                (virtualRegs[0x4B] & 0x10) |
                ((fields.TEST_MUX & 0x07) << 5) |
                ((fields.SAMPLE_SEL & 0x01) << 3) |
                (fields.DP_MUX & 0x07));

            WriteRegister(0x4B, reg4B);
        }

        private void DecodeDeviceStatus()
        {
            byte reg4C = ReadRegister(0x4C);
            byte reg4D = ReadRegister(0x4D);

            fields.REV_ID = (byte)((reg4C >> 6) & 0x03);
            fields.OPT_ID = (byte)((reg4C >> 4) & 0x03);
            fields.CMW_WU_ERR = (byte)((reg4C >> 3) & 0x01);
            fields.THR_CRC_ERR = (byte)((reg4C >> 2) & 0x01);
            fields.EE_CRC_ERR = (byte)((reg4C >> 1) & 0x01);
            fields.TRIM_CRC_ERR = (byte)(reg4C & 0x01);

            fields.TSD_PROT = (byte)((reg4D >> 6) & 0x01);
            fields.IOREG_OV = (byte)((reg4D >> 5) & 0x01);
            fields.IOREG_UV = (byte)((reg4D >> 4) & 0x01);
            fields.AVDD_OV = (byte)((reg4D >> 3) & 0x01);
            fields.AVDD_UV = (byte)((reg4D >> 2) & 0x01);
            fields.VPWR_OV = (byte)((reg4D >> 1) & 0x01);
            fields.VPWR_UV = (byte)(reg4D & 0x01);
        }

        private void DecodeP1Threshold()
        {
            byte reg5F = ReadRegister(0x5F);
            byte reg60 = ReadRegister(0x60);
            byte reg61 = ReadRegister(0x61);
            byte reg62 = ReadRegister(0x62);
            byte reg63 = ReadRegister(0x63);
            byte reg64 = ReadRegister(0x64);
            byte reg65 = ReadRegister(0x65);
            byte reg66 = ReadRegister(0x66);
            byte reg67 = ReadRegister(0x67);
            byte reg68 = ReadRegister(0x68);
            byte reg69 = ReadRegister(0x69);

            fields.TH_P1_T1 = (byte)((reg5F >> 4) & 0x0F);
            fields.TH_P1_T2 = (byte)(reg5F & 0x0F);
            fields.TH_P1_T3 = (byte)((reg60 >> 4) & 0x0F);
            fields.TH_P1_T4 = (byte)(reg60 & 0x0F);
            fields.TH_P1_T5 = (byte)((reg61 >> 4) & 0x0F);
            fields.TH_P1_T6 = (byte)(reg61 & 0x0F);
            fields.TH_P1_T7 = (byte)((reg62 >> 4) & 0x0F);
            fields.TH_P1_T8 = (byte)(reg62 & 0x0F);
            fields.TH_P1_T9 = (byte)((reg63 >> 4) & 0x0F);
            fields.TH_P1_T10 = (byte)(reg63 & 0x0F);
            fields.TH_P1_T11 = (byte)((reg64 >> 4) & 0x0F);
            fields.TH_P1_T12 = (byte)(reg64 & 0x0F);

            fields.TH_P1_L1 = (byte)((reg65 >> 3) & 0x1F);
            fields.TH_P1_L2 = (byte)(((reg65 & 0x07) << 2) | ((reg66 >> 6) & 0x03));
            fields.TH_P1_L3 = (byte)((reg66 >> 1) & 0x1F);
            fields.TH_P1_L4 = (byte)(((reg66 & 0x01) << 4) | ((reg67 >> 4) & 0x0F));
            fields.TH_P1_L5 = (byte)(((reg67 & 0x0F) << 1) | ((reg68 >> 7) & 0x01));
            fields.TH_P1_L6 = (byte)((reg68 >> 2) & 0x1F);
            fields.TH_P1_L7 = (byte)(((reg68 & 0x03) << 3) | ((reg69 >> 5) & 0x07));
            fields.TH_P1_L8 = (byte)(reg69 & 0x1F);

            fields.TH_P1_L9 = ReadRegister(0x6A);
            fields.TH_P1_L10 = ReadRegister(0x6B);
            fields.TH_P1_L11 = ReadRegister(0x6C);
            fields.TH_P1_L12 = ReadRegister(0x6D);
            fields.TH_P1_OFF = (byte)(ReadRegister(0x6E) & 0x0F);
        }

        private void EncodeP1Threshold()
        {
            WriteRegister(0x5F, (byte)(((fields.TH_P1_T1 & 0x0F) << 4) | (fields.TH_P1_T2 & 0x0F)));
            WriteRegister(0x60, (byte)(((fields.TH_P1_T3 & 0x0F) << 4) | (fields.TH_P1_T4 & 0x0F)));
            WriteRegister(0x61, (byte)(((fields.TH_P1_T5 & 0x0F) << 4) | (fields.TH_P1_T6 & 0x0F)));
            WriteRegister(0x62, (byte)(((fields.TH_P1_T7 & 0x0F) << 4) | (fields.TH_P1_T8 & 0x0F)));
            WriteRegister(0x63, (byte)(((fields.TH_P1_T9 & 0x0F) << 4) | (fields.TH_P1_T10 & 0x0F)));
            WriteRegister(0x64, (byte)(((fields.TH_P1_T11 & 0x0F) << 4) | (fields.TH_P1_T12 & 0x0F)));

            WriteRegister(0x65, (byte)(((fields.TH_P1_L1 & 0x1F) << 3) | ((fields.TH_P1_L2 >> 2) & 0x07)));
            WriteRegister(0x66, (byte)(((fields.TH_P1_L2 & 0x03) << 6) | ((fields.TH_P1_L3 & 0x1F) << 1) | ((fields.TH_P1_L4 >> 4) & 0x01)));
            WriteRegister(0x67, (byte)(((fields.TH_P1_L4 & 0x0F) << 4) | ((fields.TH_P1_L5 >> 1) & 0x0F)));
            WriteRegister(0x68, (byte)(((fields.TH_P1_L5 & 0x01) << 7) | ((fields.TH_P1_L6 & 0x1F) << 2) | ((fields.TH_P1_L7 >> 3) & 0x03)));
            WriteRegister(0x69, (byte)(((fields.TH_P1_L7 & 0x07) << 5) | (fields.TH_P1_L8 & 0x1F)));

            WriteRegister(0x6A, fields.TH_P1_L9);
            WriteRegister(0x6B, fields.TH_P1_L10);
            WriteRegister(0x6C, fields.TH_P1_L11);
            WriteRegister(0x6D, fields.TH_P1_L12);

            WriteRegister(0x6E, (byte)((virtualRegs[0x6E] & 0xF0) | (fields.TH_P1_OFF & 0x0F)));
        }

        private byte[] BuildThresholdBlockBytes()
        {
            byte[] data = new byte[32];

            // --- P1 threshold: registri 0x5F-0x6E (16 byte) ---
            data[0] = (byte)(((fields.TH_P1_T1 & 0x0F) << 4) | (fields.TH_P1_T2 & 0x0F));   // 0x5F
            data[1] = (byte)(((fields.TH_P1_T3 & 0x0F) << 4) | (fields.TH_P1_T4 & 0x0F));   // 0x60
            data[2] = (byte)(((fields.TH_P1_T5 & 0x0F) << 4) | (fields.TH_P1_T6 & 0x0F));   // 0x61
            data[3] = (byte)(((fields.TH_P1_T7 & 0x0F) << 4) | (fields.TH_P1_T8 & 0x0F));   // 0x62
            data[4] = (byte)(((fields.TH_P1_T9 & 0x0F) << 4) | (fields.TH_P1_T10 & 0x0F));  // 0x63
            data[5] = (byte)(((fields.TH_P1_T11 & 0x0F) << 4) | (fields.TH_P1_T12 & 0x0F)); // 0x64

            data[6] = (byte)(((fields.TH_P1_L1 & 0x1F) << 3) | ((fields.TH_P1_L2 >> 2) & 0x07));                                   // 0x65
            data[7] = (byte)(((fields.TH_P1_L2 & 0x03) << 6) | ((fields.TH_P1_L3 & 0x1F) << 1) | ((fields.TH_P1_L4 >> 4) & 0x01)); // 0x66
            data[8] = (byte)(((fields.TH_P1_L4 & 0x0F) << 4) | ((fields.TH_P1_L5 >> 1) & 0x0F));                                   // 0x67
            data[9] = (byte)(((fields.TH_P1_L5 & 0x01) << 7) | ((fields.TH_P1_L6 & 0x1F) << 2) | ((fields.TH_P1_L7 >> 3) & 0x03)); // 0x68
            data[10] = (byte)(((fields.TH_P1_L7 & 0x07) << 5) | (fields.TH_P1_L8 & 0x1F));                                          // 0x69

            data[11] = fields.TH_P1_L9;   // 0x6A
            data[12] = fields.TH_P1_L10;  // 0x6B
            data[13] = fields.TH_P1_L11;  // 0x6C
            data[14] = fields.TH_P1_L12;  // 0x6D
            data[15] = (byte)((virtualRegs[0x6E] & 0xF0) | (fields.TH_P1_OFF & 0x0F)); // 0x6E

            // --- P2 threshold: registri 0x6F-0x7E (16 byte) ---
            // Non esistono campi dedicati per P2 in questo progetto: si riusano
            // i valori correnti in cache (letti dal device o dai default).
            for (int i = 0; i < 16; i++)
                data[16 + i] = virtualRegs[0x6F + i];

            return data;
        }

        // Costruisce il frame UART: 0x55, CMD(16), 32 byte dati, checksum.
        // Nessun byte di indirizzo registro: il bulk write parte implicitamente
        // dal primo registro di soglia (0x5F).
        private byte[] BuildThresholdBulkWriteFrame(byte[] data32)
        {
            if (data32 == null || data32.Length != 32)
                throw new ArgumentException("Il Threshold bulk write richiede esattamente 32 byte di dati.");

            byte cmd = BuildCommand(0x10); // CMD[4:0] = 16 = Threshold bulk write

            byte[] payload = new byte[1 + data32.Length];
            payload[0] = cmd;
            Array.Copy(data32, 0, payload, 1, data32.Length);

            byte cs = CalcChecksum(payload);

            byte[] frame = new byte[1 + payload.Length + 1];
            frame[0] = 0x55;
            Array.Copy(payload, 0, frame, 1, payload.Length);
            frame[frame.Length - 1] = cs;

            return frame;
        }

        // Esegue il Threshold bulk write vero e proprio (CMD 16).
        private void ThresholdBulkWrite()
        {
            byte[] data = BuildThresholdBlockBytes();

            if (offlineMode)
            {
                for (int i = 0; i < 32; i++)
                {
                    WriteVirtualRegister((byte)(0x5F + i), data[i]);
                    virtualRegs[0x5F + i] = data[i];
                }
                Debug.WriteLine("[ThresholdBulkWrite] (offline) 32 byte soglie aggiornati in RAM virtuale");
                return;
            }

            if (serial == null || !serial.IsOpen)
                throw new Exception("Connettere prima la porta COM");

            byte[] frame = BuildThresholdBulkWriteFrame(data);

            lock (serialLock)
            {
                serial.DiscardInBuffer();
                serial.Write(frame, 0, frame.Length);

                // Il PGA460 impiega qualche ms per processare il bulk write
                // e ricalcolare il CRC interno delle soglie.
                Thread.Sleep(5);
            }

            // Allinea la cache RAM locale ai valori appena trasmessi
            for (int i = 0; i < 32; i++)
                virtualRegs[0x5F + i] = data[i];

            Debug.WriteLine("[ThresholdBulkWrite] inviati 32 byte soglie via CMD 16 (bulk write)");
        }

        // Handler da agganciare a un pulsante (es. "btnWriteThreshold") nel Designer,
        // con lo stesso pattern di: btnWriteEEPROM.Click += btnWriteEEPROM_Click;
        //
        //     btnWriteThreshold.Click += btnWriteThreshold_Click;
        //
        private void btnWriteThreshold_Click(object sender, EventArgs e)
        {
            try
            {
                ThresholdBulkWrite();

                // Rilegge il registro diagnostico 0x4C per verificare che
                // THR_CRC_ERR (bit 2) sia effettivamente tornato a 0.
                byte reg4C = ReadRegister(0x4C);
                fields.THR_CRC_ERR = (byte)((reg4C >> 2) & 0x01);

                UpdateFieldsGrid();

                if (fields.THR_CRC_ERR == 0)
                    MessageBox.Show("Soglie scritte con CMD 16 (bulk write). THR_CRC_ERR = 0.");
                else
                    MessageBox.Show("Soglie scritte, ma THR_CRC_ERR risulta ancora a 1.\n" +
                                     "Verificare che tutti i 32 byte trasmessi (P1 + P2) siano corretti.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[btnWriteThreshold_Click] errore: " + ex.Message);
                MessageBox.Show("Errore durante il bulk write delle soglie:\n" + ex.Message);
            }
        }


        private static System.Timers.Timer burst_interval;
        private static System.Timers.Timer burst_to_dump;
        private static System.Timers.Timer burst_decoding;
        private static System.Timers.Timer misura_to_result;

        public Form1()
        {
            InitializeComponent();

            this.Load += Form1_Load;

            try
            {
                numDistanceSet.Minimum = 0;
                numDistanceSet.Maximum = 10000;

                numWidthSet.Minimum = 0;
                numWidthSet.Maximum = 255;

                numAmplitudeSet.Minimum = 0;
                numAmplitudeSet.Maximum = 255;

                chkObjectDetect.Checked = true;

                LoadComPorts();

                cmbCom.SelectedIndexChanged += cmbCom_SelectedIndexChanged;

                InitGrid();
                LoadRegisters();

                InitFieldsGrid();

                offlineMode = true;
                LoadDefaultRegisters();
                DecodeRegisters14To6E();

                VirtualRegistersToGrid();
                UpdateFieldsGrid();

                panelTrigger.BackColor = Color.LightGray;

                panelTrigger.BorderStyle = BorderStyle.FixedSingle;

                btnScanGrafico.Click += btnScanGrafico_Click;

                scanTimer.Interval = 1000;

                btnReadConfig.Click += btnReadConfig_Click;

                btnSaveConfigFile.Click += btnSaveConfigFile_Click;
                btnLoadConfigFile.Click += btnLoadConfigFile_Click;

                burst_to_dump = new System.Timers.Timer(50);      // intervallo in ms

                burst_to_dump.AutoReset = false;       // ripete automaticamente

                burst_to_dump.Elapsed += burst_to_dump_Elapsed;   // handler dell’evento

                burst_decoding = new System.Timers.Timer(200);

                burst_decoding.AutoReset = false;       // ripete automaticamente

                burst_decoding.Elapsed += burst_decoding_Elapsed;  // handler dell’evento

                burst_interval = new System.Timers.Timer(SCAN_PHASE_INTERVAL_MS);      // intervallo in ms (300ms)

                burst_interval.Elapsed += burst_interval_Elapsed;

                burst_interval.AutoReset = true;       // ripete automaticamente: scandisce il cambio fase Grafico/Misure

                // --- Sezione MISURE: timer indipendenti, non toccano quelli sopra ---
                misura_to_result = new System.Timers.Timer(50);      // intervallo in ms

                misura_to_result.AutoReset = false;

                misura_to_result.Elapsed += misura_to_result_Elapsed;

                dgvFields.CellEndEdit += dgvFields_CellEndEdit;

                txtIndirizzo.TextChanged += txtIndirizzo_TextChanged;

                btnWriteEEPROM.Click += btnWriteEEPROM_Click;

                btnWriteThreshold.Click += btnWriteThreshold_Click;

            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Form1 ctor] initialization error: " + ex.ToString());
                MessageBox.Show("Errore inizializzazione: " + ex.ToString());
                // Do not rethrow to avoid terminating the application; allow user to inspect UI and logs.
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                refreshGraph = true;
                SafeDrawDump(lastEchoDump);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Form1_Load] errore nel disegno iniziale del grafico: " + ex.Message);
            }
        }

        // Avvia la fase "scan grafico": reg 0x40 = 0x80, invia il burst e
        // fa partire la catena burst_to_dump -> burst_decoding per leggere il dump.
        private void StartGraficoPhase()
        {
            currentScanPhase = ScanPhase.Grafico;

            scanEnabled = true;
            misureScanEnabled = false;

            WriteRegister(0x40, 0x80, force: true);

            SendBurst();

            burst_to_dump.Interval = 50;
            burst_to_dump.Start();
        }

        // Avvia la fase "scan misure": reg 0x40 = 0x00, invia il burst e
        // fa partire misura_to_result per leggere la misura.
        private void StartMisurePhase()
        {
            currentScanPhase = ScanPhase.Misure;

            misureScanEnabled = true;
            scanEnabled = false;

            WriteRegister(0x40, 0x00, force: true);

            SendBurst();

            misura_to_result.Interval = 50;
            misura_to_result.Start();
        }

        // Svuota il buffer seriale in ingresso tra una fase e l'altra.
        private void FlushSerialBuffer()
        {
            try
            {
                lock (serialLock)
                {
                    if (serial != null && serial.IsOpen)
                        serial.DiscardInBuffer();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[FlushSerialBuffer] errore: " + ex.Message);
            }
        }

        // Scandisce ogni SCAN_PHASE_INTERVAL_MS (300ms) il cambio di fase:
        // Grafico -> svuota buffer -> Misure -> svuota buffer -> Grafico -> ... in modo ciclico,
        // finché il pulsante non ferma lo scan (combinedScanEnabled = false).
        private void burst_interval_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!combinedScanEnabled)
                return;

            graficoRequested = true;
        }

        private void misura_to_result_Elapsed(object sender, ElapsedEventArgs e)
        {
            misura_to_result.Enabled = false;

            byte[] rx = ReadMeasurement();

            if (rx != null)
            {
                BeginInvoke(new System.Windows.Forms.MethodInvoker(() =>
                {
                    DecodeMeasurement(rx);
                }));
            }

            if (!combinedScanEnabled)
                return;

            try
            {
                FlushSerialBuffer();

                if (graficoRequested)
                {
                    graficoRequested = false;
                    StartGraficoPhase();
                }
                else
                {
                    StartMisurePhase();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[misura_to_result_Elapsed] connessione persa, fermo lo scan: " + ex.Message);

                BeginInvoke(new System.Windows.Forms.MethodInvoker(() =>
                {
                    StopCombinedScan();
                    MessageBox.Show("Connessione al PGA460 persa: scansione interrotta.");
                }));
            }
        }

        private void burst_to_dump_Elapsed(object sender, ElapsedEventArgs e)
        {
            burst_to_dump.Enabled = false;

            byte cmd = BuildCommand(0x07);

            byte[] d = { cmd };

            byte cs = CalcChecksum(d);

            byte[] frame =
            {
        0x55,
        cmd,
        cs
    };

            try
            {
                lock (serialLock)
                {
                    serial.DiscardInBuffer();
                    serial.Write(frame, 0, frame.Length);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            burst_decoding.Interval = 150;
            burst_decoding.Start();

        }

        private byte[] ReadEchoDump()
        {

            const int PACKET_SIZE = 130;

            byte[] rx = new byte[PACKET_SIZE];

            int received = 0;

            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

            lock (serialLock)
            {
                while (received < PACKET_SIZE)
                {
                    // Timeout dopo 500 ms
                    if (sw.ElapsedMilliseconds > 500)
                    {
                        Debug.WriteLine($"[ReadEchoDump] Timeout. Ricevuti {received} byte(s)");

                        return null;
                    }

                    int available = serial.BytesToRead;

                    if (available > 0)
                    {
                        int n = serial.Read(
                            rx,
                            received,
                            PACKET_SIZE - received);

                        received += n;
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
            }
            return rx;
        }

        private void burst_decoding_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!scanEnabled)
                return;

            burst_decoding.Enabled = false;


            byte[] dump = ReadEchoDump();

            if (dump != null)
                Debug.WriteLine(BitConverter.ToString(dump.Take(16).ToArray()));

            if (dump == null || dump.Length < 130)
            {
                Debug.WriteLine("[burst_decoding] Dump non valido o incompleto");
                MessageBox.Show("Dump non valido");
                ResumeMisureLoopAfterGrafico();
                return;
            }


            // 4) Copia i 128 campioni
            byte[] samples = new byte[128];

            Array.Copy(dump, 1, samples, 0, 128);


            // 5) Disegna grafico
            // diagnostic logging: sample stats
            try
            {
                byte min = 255, max = 0;

                for (int i = 0; i < samples.Length; i++)
                {
                    if (samples[i] < min) min = samples[i];
                    if (samples[i] > max) max = samples[i];
                }

                Debug.WriteLine($"[DUMP] samples len={samples.Length} first={samples[0]} mid={samples[samples.Length / 2]} last={samples[samples.Length - 1]} min={min} max={max}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[DUMP] diagnostic error: " + ex.Message);
            }

            lastEchoDump = (byte[])samples.Clone();

            SafeDrawDump(lastEchoDump);

            ResumeMisureLoopAfterGrafico();
        }

        private void ResumeMisureLoopAfterGrafico()
        {
            if (!combinedScanEnabled)
                return;

            try
            {
                FlushSerialBuffer();
                StartMisurePhase();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ResumeMisureLoopAfterGrafico] connessione persa, fermo lo scan: " + ex.Message);

                BeginInvoke(new System.Windows.Forms.MethodInvoker(() =>
                {
                    StopCombinedScan();
                    MessageBox.Show("Connessione al PGA460 persa: scansione interrotta.");
                }));
            }
        }

        private void LoadComPorts()
        {
            cmbCom.Items.Clear();

            string[] ports = SerialPort.GetPortNames();

            Array.Sort(ports);

            cmbCom.Items.AddRange(ports);

            if (cmbCom.Items.Count > 0)
                cmbCom.SelectedIndex = 0;
        }

        private byte DetectUartAddress()
        {
            for (byte addr = 0; addr <= 7; addr++)
            {
                currentUartAddress = addr;

                try
                {
                    ReadRegister(0x1F);
                    Debug.WriteLine($"[DetectUartAddress] dispositivo trovato all'indirizzo {addr}");
                    return addr;
                }
                catch (Exception)
                {
                    // Nessuna risposta a questo indirizzo: provo il successivo.
                }
            }
            currentUartAddress = 0;
            throw new Exception("Nessun PGA460 ha risposto (indirizzi 0-7). Verificare cablaggio/alimentazione.");
        }

        private void cmbCom_SelectedIndexChanged(object sender, EventArgs e)
        {
            ConnettiPortaSelezionata();
        }

        private void ConnettiPortaSelezionata()
        {
            if (string.IsNullOrEmpty(cmbCom.Text))
                return;

            try
            {
                if (serial != null &&
                    serial.IsOpen)
                {
                    serial.Close();
                }

                serial = new SerialPort(cmbCom.Text, 9600, Parity.None, 8, StopBits.Two);

                serial.ReadTimeout = 500;
                serial.WriteTimeout = 500;

                serial.Open();

                // Rilevo l'indirizzo UART del PGA460 collegato: serve comunicare
                // realmente con il dispositivo, quindi esco temporaneamente
                // dalla modalità "solo area dati PC".
                offlineMode = false;

                byte detectedAddress = DetectUartAddress();
                currentUartAddress = detectedAddress;
                


                txtIndirizzo.Text = detectedAddress.ToString();

                // NOTA: la connessione NON ricarica i dati di default e non
                // trasmette/legge automaticamente i registri: l'area dati PC
                // resta quella corrente (di default all'avvio, da file se
                // caricata, o modificata dall'utente) finché non si usa
                // esplicitamente "Trasmetti a PGA" o "Leggi da PGA".

                MessageBox.Show($"Connesso a {cmbCom.Text}");
            }
            catch (Exception ex)
            {
                offlineMode = true;

                if (serial != null && serial.IsOpen)
                    serial.Close();

                MessageBox.Show("Errore connessione:\n" + ex.Message);
            }
        }

        private void btnReadConfig_Click(object sender, EventArgs e)
        {
            try
            {
                if (offlineMode)
                    GridToVirtualRegisters();

                DecodeRegisters14To6E();
                UpdateFieldsGrid();

                refreshGraph = true;

                if (lastEchoDump != null)
                    SafeDrawDump(lastEchoDump);

                MessageBox.Show(offlineMode ? "Decode completato" : "Configurazione letta dal PGA460");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void WriteConfiguration()
        {
            ReadFieldsGrid();

            forceRegisterWrite = true;

            try
            {
                EncodeRegisters14To6E();
            }
            finally
            {
                forceRegisterWrite = false;
            }

            refreshGraph = true;

            if (lastEchoDump != null)
                DrawDump(lastEchoDump);
        }

        private void dgvFields_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            WriteConfiguration();
        }

        private void InitGrid()
        {
            dgvRegisters.Columns.Clear();

            dgvRegisters.Columns.Add("Addr", "Addr");

            dgvRegisters.Columns.Add("Name", "Nome Registro");

            dgvRegisters.Columns.Add("Value", "HEX");

            DataGridViewButtonColumn btn = new DataGridViewButtonColumn();

            btn.Name = "Write";
            btn.HeaderText = "Scrivi";
            btn.Text = "Write";
            btn.UseColumnTextForButtonValue = true;

            dgvRegisters.Columns.Add(btn);

            dgvRegisters.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dgvRegisters.AllowUserToAddRows = false;
        }

        private void AddRegister(byte addr, string name)
        {
            dgvRegisters.Rows.Add(addr.ToString("X2"), name, "00");
        }

        private void LoadRegisters()
        {
            AddRegister(0x14, "TVGAIN0");
            AddRegister(0x15, "TVGAIN1");
            AddRegister(0x16, "TVGAIN2");
            AddRegister(0x17, "TVGAIN3");
            AddRegister(0x18, "TVGAIN4");
            AddRegister(0x19, "TVGAIN5");
            AddRegister(0x1A, "TVGAIN6");

            AddRegister(0x1B, "INIT_GAIN");
            AddRegister(0x1C, "FREQUENCY");
            AddRegister(0x1D, "DEADTIME");

            AddRegister(0x1E, "PULSE_P1");
            AddRegister(0x1F, "PULSE_P2");

            AddRegister(0x20, "CURR_LIM_P1");
            AddRegister(0x21, "CURR_LIM_P2");

            AddRegister(0x22, "REC_LENGTH");
            AddRegister(0x23, "FREQ_DIAG");
            AddRegister(0x24, "SAT_FDIAG_TH");
            AddRegister(0x25, "FVOLT_DEC");
            AddRegister(0x26, "DECPL_TEMP");

            AddRegister(0x27, "DSP_SCALE");
            AddRegister(0x28, "TEMP_TRIM");

            AddRegister(0x29, "P1_GAIN_CTRL");
            AddRegister(0x2A, "P2_GAIN_CTRL");

            AddRegister(0x2B, "EE_CRC");

            AddRegister(0x40, "EE_CNTRL");

            AddRegister(0x4B, "TEST_MUX");
            AddRegister(0x4C, "DEV_STAT0");
            AddRegister(0x4D, "DEV_STAT1");

            AddRegister(0x41, "BPF_A2_MSB");
            AddRegister(0x42, "BPF_A2_LSB");
            AddRegister(0x43, "BPF_A3_MSB");
            AddRegister(0x44, "BPF_A3_LSB");
            AddRegister(0x45, "BPF_B1_MSB");
            AddRegister(0x46, "BPF_B1_LSB");

            AddRegister(0x47, "LPF_A2_MSB");
            AddRegister(0x48, "LPF_A2_LSB");
            AddRegister(0x49, "LPF_B1_MSB");
            AddRegister(0x4A, "LPF_B1_LSB");

            for (byte a = 0x5F; a <= 0x6E; a++)
            {
                AddRegister(a, $"P1_THR_{a - 0x5F}");
            }

            for (byte a = 0x6F; a <= 0x7E; a++)
            {
                AddRegister(a, $"P2_THR_{a - 0x6F}");
            }
        }

        private byte ReadVirtualRegister(byte addr)
        {
            return virtualRegs[addr];
        }

        private void WriteVirtualRegister(byte addr, byte value)
        {
            virtualRegs[addr] = value;
        }


        private byte ReadRegister(byte addr)
        {
            if (offlineMode)
                return ReadVirtualRegister(addr);

            if (serial == null || !serial.IsOpen)
                throw new Exception("Connettere prima la porta COM");

            byte[] frame = BuildRead(addr);

            lock (serialLock)
            {
                serial.DiscardInBuffer();

                serial.Write(frame, 0, frame.Length);

                Stopwatch sw = Stopwatch.StartNew();

                while (serial.BytesToRead < 3)
                {
                    if (sw.ElapsedMilliseconds > 100)
                        throw new Exception("Timeout lettura registro");

                    Thread.Sleep(1);
                }

                byte[] rx = new byte[3];

                serial.Read(rx, 0, 3);

                byte value = rx[1];

                // cache update after successful read
                virtualRegs[addr] = value;

                Debug.WriteLine($"[ReadRegister] addr=0x{addr:X2} value=0x{value:X2}");

                return value;
            }
        }


        private void WriteRegister(byte addr, byte value, bool force = false)
        {
            // For offline mode just update virtual registers
            if (offlineMode)
            {
                WriteVirtualRegister(addr, value);
                virtualRegs[addr] = value;
                Debug.WriteLine($"[WriteRegister] (offline) addr=0x{addr:X2} value=0x{value:X2}");
                return;
            }

            if (serial == null || !serial.IsOpen)
                throw new Exception("Connettere prima la porta COM");

            if (!force && !forceRegisterWrite)
            {
                // Skip physical write when cached value equals desired value to avoid unnecessary writes
                try
                {
                    if (virtualRegs[addr] == value)
                    {
                        Debug.WriteLine($"[WriteRegister] skip addr=0x{addr:X2} value=0x{value:X2} (no change)");
                        return;
                    }
                }
                catch
                {
                    // If virtualRegs access fails for any reason, proceed with write
                }
            }

            byte[] frame = BuildWrite(addr, value);

            // Perform serial write under lock and update virtualRegs only after successful write
            lock (serialLock)
            {
                try
                {
                    serial.DiscardInBuffer();
                    serial.Write(frame, 0, frame.Length);
                    Thread.Sleep(2);
                    // update cached virtual register after successful transmission
                    virtualRegs[addr] = value;

                    // diagnostic log for all writes
                    Debug.WriteLine($"[WriteRegister] addr=0x{addr:X2} value=0x{value:X2}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[WriteRegister] error: " + ex.Message);
                    throw;
                }
            }
        }

        private void GridToVirtualRegisters()
        {
            foreach (DataGridViewRow row in dgvRegisters.Rows)
            {
                byte addr = Convert.ToByte(row.Cells[0].Value.ToString(), 16);
                byte val = Convert.ToByte(row.Cells[2].Value.ToString(), 16);

                virtualRegs[addr] = val;
            }
        }

        private void VirtualRegistersToGrid()
        {
            foreach (DataGridViewRow row in dgvRegisters.Rows)
            {
                byte addr = Convert.ToByte(row.Cells[0].Value.ToString(), 16);
                row.Cells[2].Value = virtualRegs[addr].ToString("X2");
            }
        }

        private void InitFieldsGrid()
        {
            dgvFields = new DataGridView();

            dgvFields.Location = new Point(599, 25);
            dgvFields.Size = new Size(729, 255);
            dgvFields.AllowUserToAddRows = false;
            dgvFields.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            dgvFields.Columns.Add("Name", "Parametro");
            dgvFields.Columns.Add("Value", "Valore");
            dgvFields.Columns.Add("Description", "Descrizione");
            dgvFields.Columns.Add("Min", "Min");
            dgvFields.Columns.Add("Max", "Max");

            dgvFields.Columns["Description"].Width = 278;
            dgvFields.Columns["Name"].Width = 180;
            dgvFields.Columns["Value"].Width = 70;
            dgvFields.Columns["Min"].Width = 70;
            dgvFields.Columns["Max"].Width = 70;

            dgvFields.Columns[0].ReadOnly = true;
            dgvFields.Columns[2].ReadOnly = true;
            dgvFields.Columns[3].ReadOnly = true;
            dgvFields.Columns[4].ReadOnly = true;

            Dictionary<string, FieldInfoEx> fieldInfos = new Dictionary<string, FieldInfoEx>();

            fieldInfos.Add("TVG_T0", new FieldInfoEx("Punto variazione guadagno: Start time 100-8000us", "0", "15"));

            fieldInfos.Add("TVG_T1", new FieldInfoEx("Punto var. guadagno: Delta time T0/T1  100-8000us", "0", "15"));

            fieldInfos.Add("TVG_T2", new FieldInfoEx("Punto var. guadagno: Delta time T1/T2  100-8000us", "0", "15"));

            fieldInfos.Add("TVG_T3", new FieldInfoEx("Punto var. guadagno: Delta time T2/T3  100-8000us", "0", "15"));

            fieldInfos.Add("TVG_T4", new FieldInfoEx("Punto var. guadagno: Delta time T3/T4  100-8000us", "0", "15"));

            fieldInfos.Add("TVG_T5", new FieldInfoEx("Punto var. guadagno: Delta time T4/T5  100-8000us", "0", "15"));

            fieldInfos.Add("TVG_G1", new FieldInfoEx("Guadano punto 1 /gain = 0.5*(tvg_G1+1)+value (AFE_GAIN_RNG) db", "0", "63"));

            fieldInfos.Add("TVG_G2", new FieldInfoEx("Guadano punto 2 /gain = 0.5*(tvg_G2+1)+value (AFE_GAIN_RNG) db", "0", "63"));

            fieldInfos.Add("TVG_G3", new FieldInfoEx("Guadano punto 3 /gain = 0.5*(tvg_G3+1)+value (AFE_GAIN_RNG) db", "0", "63"));

            fieldInfos.Add("TVG_G4", new FieldInfoEx("Guadano punto 4 /gain = 0.5*(tvg_G4+1)+value (AFE_GAIN_RNG) db", "0", "63"));

            fieldInfos.Add("TVG_G5", new FieldInfoEx("Guadano punto5 /gain = 0.5*(tvg_G5+1)+value (AFE_GAIN_RNG) db", "0", "63"));

            fieldInfos.Add("FREQ_SHIFT", new FieldInfoEx("Burst frequency range ", "0", "1"));

            fieldInfos.Add("BPF_BW", new FieldInfoEx("Larghezza banda filtro BP     bw = 2 × (BPF_BW + 1) [kHz]", "0", "3"));

            fieldInfos.Add("GAIN_INIT", new FieldInfoEx("Guadagno iniziale Analog Front End:  0.5 × (GAIN_INIT+1) + value(AFE_GAIN_RNG) [dB]", "0", "63"));

            fieldInfos.Add("FREQ", new FieldInfoEx("Frequenza del burst:  Frequency = 0.2 × FREQ + 30 [kHz]", "0", "249"));

            fieldInfos.Add("THR_CMP_DEGLTCH", new FieldInfoEx("Periodo di DEGLITCH: = (THR_CMP_DEGLITCH × 8) [μs]", "0", "15"));

            fieldInfos.Add("PULSE_DT", new FieldInfoEx("Dead Time impulso di burst  = 0.0625 × PULSE_DT[μs]", "0", "15"));

            fieldInfos.Add("IO_IF_SEL", new FieldInfoEx("Tipo di interfaccia", "0", "0"));

            fieldInfos.Add("UART_DIAG", new FieldInfoEx("UART Diagnostic Page Selection:", "0", "0"));

            fieldInfos.Add("IO_DIS", new FieldInfoEx("Disable IO pin transceiver:", "0", "0"));

            fieldInfos.Add("P1_PULSE", new FieldInfoEx("Numero di impulsi di burst per P1", "1", "32"));

            fieldInfos.Add("UART_ADDR", new FieldInfoEx("Indirizzo interfaccia UART", "0", "7"));

            fieldInfos.Add("P2_PULSE", new FieldInfoEx("Numero di impulsi di burst per P2", "1", "32"));

            fieldInfos.Add("DIS_CL", new FieldInfoEx("Disabilita il CURRENT LIMITER", "0", "0"));

            fieldInfos.Add("CURR_LIM1", new FieldInfoEx("Limite corrente Preset 1        =7 × CURR_LIM1 + 50 [mA]", "0", "63"));

            fieldInfos.Add("LPF_CO", new FieldInfoEx("Frequenza di CuTOFF del filtro Passa basso", "0", "3"));

            fieldInfos.Add("CURR_LIM2", new FieldInfoEx("Limite corrente Preset 2      = 7 × CURR_LIM1 + 50 [mA]", "0", "63"));

            fieldInfos.Add("P1_REC", new FieldInfoEx("Lunghezza del record time Preset P1     = 4.096 × (P1_REC + 1) [ms]", "0", "15"));

            fieldInfos.Add("P2_REC", new FieldInfoEx("Lunghezza del record time Preset P2     = 4.096 × (P2_REC + 1) [ms]", "0", "15"));

            fieldInfos.Add("FDIAG_LEN", new FieldInfoEx("Frequency diagnostic window length", "0", "0"));

            fieldInfos.Add("FDIAG_START", new FieldInfoEx("Frequency diagnostic start time:", "0", "0"));

            fieldInfos.Add("FDIAG_ERR_TH", new FieldInfoEx("Frequency diagnostic absolute error time threshold:", "0", "0"));

            fieldInfos.Add("SAT_TH", new FieldInfoEx("Saturation diagnostic threshold level.", "0", "0"));

            fieldInfos.Add("P1_NLS_EN", new FieldInfoEx("Set high to enable Preset1 non-linear scaling", "0", "0"));

            fieldInfos.Add("P2_NLS_EN", new FieldInfoEx("Set high to enable Preset2 non-linear scaling", "0", "0"));

            fieldInfos.Add("VPWR_OV_TH", new FieldInfoEx("VPWR over voltage threshold select:", "0", "0"));

            fieldInfos.Add("LPM_TMR", new FieldInfoEx("Low power mode enter time:", "0", "0"));

            fieldInfos.Add("FVOLT_ERR_TH", new FieldInfoEx("See section on System Diagnostics", "0", "0"));

            fieldInfos.Add("AFE_GAIN_RNG", new FieldInfoEx("Analog front End Gain Range 58-90; 52-84/46-78;32-64", "0", "3"));

            fieldInfos.Add("LPM_EN", new FieldInfoEx("Low Power Mode enable", "0", "0"));

            fieldInfos.Add("DECPL_TEMP_SEL", new FieldInfoEx("Decouple Time / Temperature Select:", "0", "0"));

            fieldInfos.Add("DECPL_T", new FieldInfoEx("Secondary decouple time / temperature decouple", "0", "0"));

            fieldInfos.Add("NOISE_LVL", new FieldInfoEx("Value ranges from 0 to 31 with 1 LSB steps for digital gain values", "0", "0"));

            fieldInfos.Add("SCALE_K", new FieldInfoEx("Non-Linear scaling exponent selection:", "0", "0"));

            fieldInfos.Add("SCALE_N", new FieldInfoEx("Non linear gain start point", "0", "0"));

            fieldInfos.Add("TEMP_GAIN", new FieldInfoEx("Temperature scaling gain:", "0", "0"));

            fieldInfos.Add("TEMP_OFF", new FieldInfoEx("Temperature Scaling Offset:", "0", "0"));

            fieldInfos.Add("P1_DIG_GAIN_LR_ST", new FieldInfoEx("Soglia Long range", "0", "0"));

            fieldInfos.Add("P1_DIG_GAIN_LR", new FieldInfoEx("Guadagno Long Range", "0", "0"));

            fieldInfos.Add("P1_DIG_GAIN_SR", new FieldInfoEx("P1 digital SR  *1  --- *32 ", "0", "5"));

            fieldInfos.Add("P2_DIG_GAIN_LR_ST", new FieldInfoEx("Selects the starting Preset2 threshold", "0", "0"));

            fieldInfos.Add("P2_DIG_GAIN_LR", new FieldInfoEx("Preset2 Digital long range (LR)", "0", "0"));

            fieldInfos.Add("P2_DIG_GAIN_SR", new FieldInfoEx("Preset2 Digital short range (SR)", "0", "0"));

            fieldInfos.Add("EE_CRC", new FieldInfoEx("User EEPROM space data CRC value", "0", "0"));

            fieldInfos.Add("DATADUMP_EN", new FieldInfoEx("Abilita il Data Dump per grafico", "0", "1"));

            fieldInfos.Add("EE_UNLCK", new FieldInfoEx("Eeprom Unlock      passcode  =    0xD", "0", "0x0D"));

            fieldInfos.Add("EE_PRGM_OK", new FieldInfoEx("Programming status.  Solo lettura", "0", "1"));

            fieldInfos.Add("EE_RLOAD", new FieldInfoEx("EEprom reload trigger ", "", ""));

            fieldInfos.Add("EE_PRGM", new FieldInfoEx("Eeprom program trigger", "0", "1"));

            fieldInfos.Add("BPF_A2_MSB", new FieldInfoEx("Bandpass filter A2 MSB", "0", "255"));

            fieldInfos.Add("BPF_A2_LSB", new FieldInfoEx("Bandpass filter A2 LSB", "0", "255"));

            fieldInfos.Add("BPF_A3_MSB", new FieldInfoEx("Bandpass filter A3 MSB", "0", "255"));

            fieldInfos.Add("BPF_A3_LSB", new FieldInfoEx("Bandpass filter A3 LSB", "0", "255"));

            fieldInfos.Add("BPF_B1_MSB", new FieldInfoEx("Bndpass filter B1 MSB", "0", "255"));

            fieldInfos.Add("BPF_B1_LSB", new FieldInfoEx("Bndpass filter B1 LSB", "0", "255"));

            fieldInfos.Add("LPF_A2_MSB", new FieldInfoEx("Low pass filter A2 MSB", "0", "128"));

            fieldInfos.Add("LPF_A2_LSB", new FieldInfoEx("Low pass filter A2 LSB", "0", "255"));

            fieldInfos.Add("LPF_B1_MSB", new FieldInfoEx("Low pass filter B1 MSB", "0", "128"));

            fieldInfos.Add("LPF_B1_LSB", new FieldInfoEx("Low pass filter B1 LSB", "0", "255"));

            fieldInfos.Add("TEST_MUX", new FieldInfoEx("Mux test", "0", "0"));

            fieldInfos.Add("SAMPLE_SEL", new FieldInfoEx("Data path sample sel", "0", "0"));

            fieldInfos.Add("DP_MUX", new FieldInfoEx("Data Path mux", "0", "0"));

            fieldInfos.Add("REV_ID", new FieldInfoEx("Flags sola lettura ", "0", "1"));

            fieldInfos.Add("OPT_ID", new FieldInfoEx("Flags sola lettura ", "0", "1"));

            fieldInfos.Add("CMW_WU_ERR", new FieldInfoEx("Flags sola lettura ", "0", "1"));

            fieldInfos.Add("THR_CRC_ERR", new FieldInfoEx("Flags sola lettura ", "0", "1"));

            fieldInfos.Add("EE_CRC_ERR", new FieldInfoEx("Flags sola lettura ", "0", "1"));

            fieldInfos.Add("TRIM_CRC_ERR", new FieldInfoEx("Flags sola lettura ", "0", "1"));

            fieldInfos.Add("TSD_PROT", new FieldInfoEx("Flags sola lettura ", "0", "1"));

            fieldInfos.Add("IOREG_OV", new FieldInfoEx("Flags sola lettura ", "0", "1"));

            fieldInfos.Add("IOREG_UV", new FieldInfoEx("Flags sola lettura ", "0", "1"));

            fieldInfos.Add("AVDD_OV", new FieldInfoEx("Flags sola lettura ", "0", "1"));

            fieldInfos.Add("AVDD_UV", new FieldInfoEx("Flags sola lettura ", "0", "1"));

            fieldInfos.Add("VPWR_OV", new FieldInfoEx("Flags sola lettura ", "0", "1"));

            fieldInfos.Add("VPWR_UV", new FieldInfoEx("Flags sola lettura ", "0", "1"));

            fieldInfos.Add("TH_P1_T1", new FieldInfoEx("Trigger preset 1 Tempo attivazione (assoluto  100us -  8000us DEVE ESSERE INIZIALIZZATO", "0", "15"));

            fieldInfos.Add("TH_P1_T2", new FieldInfoEx("P1  trigger T2 delta time  (relativo  100us -  8000us DEVE ESSERE INIZIALIZZATO", "0", "15"));

            fieldInfos.Add("TH_P1_T3", new FieldInfoEx("P1  trigger T3 delta time  (relativo  100us -  8000us DEVE ESSERE INIZIALIZZATO", "0", "15"));

            fieldInfos.Add("TH_P1_T4", new FieldInfoEx("P1  trigger T4 delta time  (relativo  100us -  8000us DEVE ESSERE INIZIALIZZATO", "0", "15"));

            fieldInfos.Add("TH_P1_T5", new FieldInfoEx("P1  trigger T5 delta time  (relativo  100us -  8000us DEVE ESSERE INIZIALIZZATO", "0", "15"));

            fieldInfos.Add("TH_P1_T6", new FieldInfoEx("P1  trigger T6 delta time  (relativo  100us -  8000us DEVE ESSERE INIZIALIZZATO", "0", "15"));

            fieldInfos.Add("TH_P1_T7", new FieldInfoEx("P1  trigger T7 delta time  (relativo  100us -  8000us DEVE ESSERE INIZIALIZZATO", "0", "15"));

            fieldInfos.Add("TH_P1_T8", new FieldInfoEx("P1  trigger T8 delta time  (relativo  100us -  8000us DEVE ESSERE INIZIALIZZATO", "0", "15"));

            fieldInfos.Add("TH_P1_T9", new FieldInfoEx("P1  trigger T9 delta time  (relativo  100us -  8000us DEVE ESSERE INIZIALIZZATO", "0", "15"));

            fieldInfos.Add("TH_P1_T10", new FieldInfoEx("P1  trigger T10 delta time  (relativo  100us -  8000us DEVE ESSERE INIZIALIZZATO", "0", "15"));

            fieldInfos.Add("TH_P1_T11", new FieldInfoEx("P1  trigger T11 delta time  (relativo  100us -  8000us DEVE ESSERE INIZIALIZZATO", "0", "15"));

            fieldInfos.Add("TH_P1_T12", new FieldInfoEx("P1  trigger T12 delta time  (relativo  100us -  8000us DEVE ESSERE INIZIALIZZATO", "0", "15"));

            fieldInfos.Add("TH_P1_L1", new FieldInfoEx("preset 1 Livello trigger 1    DEVE ESSERE INIZIALIZZATO", "0", "32"));

            fieldInfos.Add("TH_P1_L2", new FieldInfoEx("preset 1 Livello trigger 2    DEVE ESSERE INIZIALIZZATO", "0", "32"));

            fieldInfos.Add("TH_P1_L3", new FieldInfoEx("preset 1 Livello trigger 3  DEVE ESSERE INIZIALIZZATO", "0", "32"));

            fieldInfos.Add("TH_P1_L4", new FieldInfoEx("preset 1 Livello trigger 4   DEVE ESSERE INIZIALIZZATO", "0", "32"));

            fieldInfos.Add("TH_P1_L5", new FieldInfoEx("preset 1 Livello trigger 5    DEVE ESSERE INIZIALIZZATO", "0", "32"));

            fieldInfos.Add("TH_P1_L6", new FieldInfoEx("preset 1 Livello trigger 6    DEVE ESSERE INIZIALIZZATO", "0", "32"));

            fieldInfos.Add("TH_P1_L7", new FieldInfoEx("preset 1 Livello trigger 7    DEVE ESSERE INIZIALIZZATO", "0", "32"));

            fieldInfos.Add("TH_P1_L8", new FieldInfoEx("preset 1 Livello trigger 8    DEVE ESSERE INIZIALIZZATO", "0", "32"));

            fieldInfos.Add("TH_P1_L9", new FieldInfoEx("preset 1 Livello trigger 9    DEVE ESSERE INIZIALIZZATO", "0", "255"));

            fieldInfos.Add("TH_P1_L10", new FieldInfoEx("preset 1 Livello trigger 10    DEVE ESSERE INIZIALIZZATO", "0", "255"));

            fieldInfos.Add("TH_P1_L11", new FieldInfoEx("preset 1 Livello trigger 11    DEVE ESSERE INIZIALIZZATO", "0", "255"));

            fieldInfos.Add("TH_P1_L12", new FieldInfoEx("preset 1 Livello trigger 12    DEVE ESSERE INIZIALIZZATO", "0", "255"));


            foreach (FieldInfo f in typeof(PGA460Fields).GetFields())
            {
                FieldInfoEx info;

                if (!fieldInfos.TryGetValue(f.Name, out info))
                {
                    info = new FieldInfoEx("", "", "");
                }

                dgvFields.Rows.Add(f.Name, "0", info.Description, info.Min, info.Max);
            }

            Controls.Add(dgvFields);
        }

        class FieldInfoEx
        {
            public string Description;
            public string Min;
            public string Max;

            public FieldInfoEx(string d, string min, string max)
            {
                Description = d;
                Min = min;
                Max = max;
            }
        }

        private void UpdateFieldsGrid()
        {
            foreach (DataGridViewRow row in dgvFields.Rows)
            {
                string name = row.Cells[0].Value.ToString();
                FieldInfo f = typeof(PGA460Fields).GetField(name);

                if (f != null)
                    row.Cells[1].Value = ((byte)f.GetValue(fields)).ToString();
            }
        }

        private void ReadFieldsGrid()
        {
            foreach (DataGridViewRow row in dgvFields.Rows)
            {
                string name = row.Cells[0].Value.ToString();
                string text = row.Cells[1].Value.ToString();

                FieldInfo f = typeof(PGA460Fields).GetField(name);

                if (f != null)
                    f.SetValue(fields, Convert.ToByte(text));
            }
        }

        private void btnReadAll_Click(object sender, EventArgs e)
        {
            try
            {
            bool connected = serial != null && serial.IsOpen;
            bool prevOfflineModeRead = offlineMode;

            if (connected)
                offlineMode = false;
                try
                {
                    foreach (
                        DataGridViewRow row in dgvRegisters.Rows)
                    {
                        byte addr = Convert.ToByte(row.Cells[0].Value.ToString(), 16);

                        byte value = ReadRegister(addr);

                        row.Cells[2].Value = value.ToString("X2");
                    }
                }
                finally
                {
                    offlineMode = prevOfflineModeRead;
                }

                bool prevOfflineMode = offlineMode;
                offlineMode = true;
                DecodeRegisters14To6E();
                offlineMode = prevOfflineMode;

                UpdateFieldsGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void WriteUartAddress(byte address)
        {
            if (address > 7)
                throw new Exception("L'indirizzo UART deve essere compreso tra 0 e 7.");

            byte reg = ReadRegister(0x1F);

            // mantiene PULSE_P2 (bit 4:0)
            reg = (byte)((reg & 0x1F) | (address << 5));

            WriteRegister(0x1F, reg);
        }

        private void btnScrivi_Click(object sender, EventArgs e)
        {
            if (serial == null || !serial.IsOpen)
            {
                MessageBox.Show("Connettere prima la porta COM");
                return;
            }

            bool prevOfflineMode = offlineMode;

            try
            {
                offlineMode = false;

                foreach (DataGridViewRow row in dgvRegisters.Rows)
                {
                    byte addr = Convert.ToByte(row.Cells[0].Value.ToString(), 16);
                    byte value = virtualRegs[addr];

                    WriteRegister(addr, value, force: true);

                    if (addr == 0x1F)
                        currentUartAddress = (byte)((value >> 5) & 0x07);
                }

                MessageBox.Show("Dati dell'area PC trasmessi al PGA460.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                offlineMode = prevOfflineMode;

                fields.UART_ADDR = currentUartAddress;
                txtIndirizzo.Text = currentUartAddress.ToString();
                UpdateFieldsGrid();
            }
        }

        private void btnLeggi_Click(object sender, EventArgs e)
        {
            if (serial == null || !serial.IsOpen)
            {
                MessageBox.Show("Connettere prima la porta COM");
                return;
            }

            bool prevOfflineMode = offlineMode;

            try
            {
                offlineMode = false;

                foreach (DataGridViewRow row in dgvRegisters.Rows)
                {
                    byte addr = Convert.ToByte(row.Cells[0].Value.ToString(), 16);
                    byte value = ReadRegister(addr);

                    row.Cells[2].Value = value.ToString("X2");
                }

                currentUartAddress = (byte)((virtualRegs[0x1F] >> 5) & 0x07);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            finally
            {
                offlineMode = prevOfflineMode;
            }

            try
            {
                // Decodifica nei campi i registri appena caricati nell'area dati PC.
                DecodeRegisters14To6E();
                UpdateFieldsGrid();

                fields.UART_ADDR = currentUartAddress;
                txtIndirizzo.Text = currentUartAddress.ToString();

                refreshGraph = true;

                if (lastEchoDump != null)
                    SafeDrawDump(lastEchoDump);

                MessageBox.Show("Dati letti dalla RAM del PGA460 e caricati nell'area dati PC.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnWriteEEPROM_Click(object sender, EventArgs e)
        {
            // Guardia anti-rientranza: se per qualsiasi motivo l'evento Click arrivasse
            // una seconda volta mentre la scrittura e' gia' in corso (es. doppio click,
            // click in coda mentre il thread UI era bloccato nei Thread.Sleep), il
            // secondo ingresso viene ignorato invece di rieseguire il trigger EEPROM.
            if (eepromWriteInProgress)
                return;

            if (offlineMode && (serial == null || !serial.IsOpen))
            {
                MessageBox.Show("Connettere prima la porta COM");
                return;
            }

            byte address;

            try
            {
                address = Convert.ToByte(txtIndirizzo.Text);
            }
            catch (Exception)
            {
                MessageBox.Show("Indirizzo UART non valido (deve essere un numero 0-7).");
                return;
            }

            // Conferma esplicita: la programmazione EEPROM del PGA460 ha un numero di
            // cicli di scrittura limitato, quindi non deve mai partire per un click
            // accidentale.
            DialogResult confirm = MessageBox.Show(
                $"Verranno programmati in EEPROM i registri attualmente in RAM, incluso l'indirizzo UART = {address}.\n\n" +
                "Questa operazione consuma un ciclo di scrittura dell'EEPROM del PGA460: procedere?",
                "Conferma scrittura EEPROM",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
                return;

            // Da qui in poi il bottone resta disabilitato e la guardia attiva finche'
            // l'intera sequenza (unlock -> trigger -> verifica) non e' terminata, cosi'
            // un secondo click fisico non puo' generare un secondo trigger di scrittura.
            eepromWriteInProgress = true;
            btnWriteEEPROM.Enabled = false;

            try
            {
                // 1) Scrive l'indirizzo UART digitato in txtIndirizzo nel registro RAM 0x1F,
                //    cosi' viene incluso nella programmazione EEPROM (mantiene P2_PULSE).
                WriteUartAddress(address);

                currentUartAddress = address;
                fields.UART_ADDR = address;

                // 2) Sblocco EEPROM: EE_UNLCK = 0xD, EE_PRGM = 0, DATADUMP_EN/EE_RLOAD = 0
                WriteRegister(0x40, 0x68, force: true);

                Thread.Sleep(2);

                // 3) Trigger di programmazione (UNA SOLA volta per click confermato):
                //    EE_UNLCK = 0xD, EE_PRGM = 1
                WriteRegister(0x40, 0x69, force: true);


                // 3b)
                byte reg40Debug = ReadRegister(0x40);

                byte dbgUnlck = (byte)((reg40Debug >> 3) & 0x0F);
                byte dbgPrgm = (byte)(reg40Debug & 0x01);
                byte dbgPrgmOk = (byte)((reg40Debug >> 2) & 0x01);

                Debug.WriteLine($"[btnWriteEEPROM_Click] subito dopo trigger: reg40=0x{reg40Debug:X2} " +
                                 $"EE_UNLCK=0x{dbgUnlck:X} EE_PRGM={dbgPrgm} EE_PRGM_OK={dbgPrgmOk}");

                // 4)

                bool ok = false;
                byte reg40 = 0;

                System.Diagnostics.Stopwatch swProg = System.Diagnostics.Stopwatch.StartNew();

                while (swProg.ElapsedMilliseconds < 2000)
                {
                    Thread.Sleep(20);

                    reg40 = ReadRegister(0x40);

                    if ((reg40 & 0x04) != 0)
                    {
                        ok = true;
                        break;
                    }
                }

                // 5) In ogni caso (successo, fallimento o timeout) si richiude il registro:
                //    EE_UNLCK = 0 (rilocca), EE_PRGM = 0 (azzera il trigger, che non si
                //    autoazzera da solo). Cosi' non si lascia il chip sbloccato ne' con il
                //    trigger ancora attivo dopo l'operazione.
                WriteRegister(0x40, 0x00, force: true);

                reg40 = ReadRegister(0x40);

                fields.DATADUMP_EN = (byte)((reg40 >> 7) & 0x01);
                fields.EE_UNLCK = (byte)((reg40 >> 3) & 0x0F);
                fields.EE_PRGM_OK = (byte)((reg40 >> 2) & 0x01);
                fields.EE_RLOAD = (byte)((reg40 >> 1) & 0x01);
                fields.EE_PRGM = (byte)(reg40 & 0x01);

                UpdateFieldsGrid();

                if (ok)
                    MessageBox.Show($"Registri programmati in EEPROM con successo (indirizzo UART = {address}).");
                else
                    MessageBox.Show("Programmazione EEPROM non riuscita: EE_PRGM_OK = 0.\n" +
                                     "Verificare cablaggio/alimentazione prima di riprovare, per non consumare cicli di scrittura inutilmente.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[btnWriteEEPROM_Click] errore: " + ex.Message);
                MessageBox.Show("Errore durante la scrittura EEPROM:\n" + ex.Message);
            }
            finally
            {
                // Riabilita sempre il bottone e rilascia la guardia, anche in caso di eccezione,
                // cosi' l'utente puo' eventualmente riprovare consapevolmente.
                eepromWriteInProgress = false;
                btnWriteEEPROM.Enabled = true;
            }
        }

        // Salva su file: registri (Addr/Value), campi decodificati (Name/Value) e le
        // ultime misure (Distanza/Dimensione/Intensità) attualmente mostrate in UI.
        private void btnSaveConfigFile_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Filter = "File configurazione PGA460 (*.pga460cfg)|*.pga460cfg|Tutti i file (*.*)|*.*";
                dlg.FileName = "config.pga460cfg";

                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                try
                {
                    SaveConfigToFile(dlg.FileName);
                    MessageBox.Show("Configurazione salvata su file.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Errore durante il salvataggio:\n" + ex.Message);
                }
            }
        }

        // Carica da file: ripopola i registri, i campi decodificati e le misure
        // nelle rispettive griglie/textbox. Non scrive nulla sul PGA460: per
        // inviare la configurazione caricata all'hardware usare "Scrivi Configurazione".
        private void btnLoadConfigFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "File configurazione PGA460 (*.pga460cfg)|*.pga460cfg|Tutti i file (*.*)|*.*";

                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                try
                {
                    LoadConfigFromFile(dlg.FileName);
                    MessageBox.Show("Configurazione caricata da file.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Errore durante il caricamento:\n" + ex.Message);
                }
            }
        }

        private void SaveConfigToFile(string path)
        {
            using (StreamWriter sw = new StreamWriter(path, false))
            {
                sw.WriteLine("; File di configurazione PGA460 - FormCollaudoPGA460");
                sw.WriteLine("; Generato il " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sw.WriteLine();

                sw.WriteLine("[Registers]");
                foreach (DataGridViewRow row in dgvRegisters.Rows)
                {
                    string addr = row.Cells[0].Value?.ToString() ?? "";
                    string name = row.Cells[1].Value?.ToString() ?? "";
                    string value = row.Cells[2].Value?.ToString() ?? "00";

                    if (addr.Length == 0)
                        continue;

                    sw.WriteLine($"{addr}={value} ; {name}");
                }

                sw.WriteLine();
                sw.WriteLine("[Fields]");
                foreach (DataGridViewRow row in dgvFields.Rows)
                {
                    string name = row.Cells[0].Value?.ToString() ?? "";
                    string value = row.Cells[1].Value?.ToString() ?? "0";

                    if (name.Length == 0)
                        continue;

                    sw.WriteLine($"{name}={value}");
                }

                sw.WriteLine();
                sw.WriteLine("[Measure]");
                sw.WriteLine($"Distance={txtDistance.Text}");
                sw.WriteLine($"Width={txtWidth.Text}");
                sw.WriteLine($"Amplitude={txtAmplitude.Text}");

                sw.WriteLine();
                sw.WriteLine("[Address]");
                sw.WriteLine($"UartAddress={txtIndirizzo.Text}");
            }
        }

        private void LoadConfigFromFile(string path)
        {
            var registers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            //var fieldsValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var measure = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var address = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            string section = "";

            foreach (string rawLine in File.ReadAllLines(path))
            {
                string line = rawLine.Trim();

                if (line.Length == 0 || line.StartsWith(";"))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    section = line.Substring(1, line.Length - 2).Trim();
                    continue;
                }

                int eq = line.IndexOf('=');
                if (eq < 0)
                    continue;

                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();

                // Rimuove un eventuale commento in coda ("; Nome registro")
                int comment = value.IndexOf(';');
                if (comment >= 0)
                    value = value.Substring(0, comment).Trim();

                switch (section)
                {
                    case "Registers":
                        registers[key] = value;
                        break;
                    /*case "Fields":
                        fieldsValues[key] = value;
                        break;*/
                    case "Measure":
                        measure[key] = value;
                        break;
                    case "Address":
                        address[key] = value;
                        break;
                }
            }

            // Ripopola la griglia registri (match per indirizzo esadecimale)
            foreach (DataGridViewRow row in dgvRegisters.Rows)
            {
                string addr = row.Cells[0].Value?.ToString() ?? "";

                if (addr.Length > 0 && registers.TryGetValue(addr, out string val))
                    row.Cells[2].Value = val;
            }

            // Ripopola le ultime misure mostrate in UI
            if (measure.TryGetValue("Distance", out string distance))
                txtDistance.Text = distance;

            if (measure.TryGetValue("Width", out string width))
                txtWidth.Text = width;

            if (measure.TryGetValue("Amplitude", out string amplitude))
                txtAmplitude.Text = amplitude;

            // Sincronizza la cache interna (virtualRegs) e l'oggetto "fields" con
            // i valori appena caricati, così i dati letti dal file sono coerenti
            // con quanto mostrato in UI, senza però inviare nulla al PGA460.
            GridToVirtualRegisters();

            bool prevOfflineModeLoad = offlineMode;
            offlineMode = true;
            DecodeRegisters14To6E();
            offlineMode = prevOfflineModeLoad;
            //ReadFieldsGrid();
            UpdateFieldsGrid();
            WriteConfiguration();
            refreshGraph = true;

            if (lastEchoDump != null)
                SafeDrawDump(lastEchoDump);

        }

        private byte GetUartAddress()
        {
            // Indirizzo attualmente valido per parlare col PGA460 (aggiornato solo dopo
            // una scrittura riuscita in WriteUartAddress). NON legge txtIndirizzo qui:
            // quel campo è per il nuovo indirizzo da programmare, non per quello corrente.
            return currentUartAddress;
        }

        private bool IndirizzoCorrisponde()
        {
            if (!byte.TryParse(txtIndirizzo.Text, out byte addr))
                return false;

            return addr == currentUartAddress;
        }

        private void txtIndirizzo_TextChanged(object sender, EventArgs e)
        {
            if (combinedScanEnabled && !IndirizzoCorrisponde())
            {
                Debug.WriteLine("[txtIndirizzo_TextChanged] indirizzo cambiato durante lo scan, fermo tutto.");
                StopCombinedScan();
            }
        }

        private byte BuildCommand(byte cmd)
        {

            byte uartAddress = GetUartAddress();

            return (byte)((cmd & 0x1F) | ((uartAddress & 0x07) << 5));
        }

        private byte[] BuildRead(byte reg)
        {
            byte cmd = BuildCommand(0x09);

            byte[] d =
            {
        cmd,
        reg
    };

            byte cs = CalcChecksum(d);

            return new byte[]
            {
        0x55,
        cmd,
        reg,
        cs
            };
        }

        private byte[] BuildWrite(byte reg, byte value)
        {
            byte cmd = BuildCommand(0x0A);

            byte[] d =
            {
        cmd,
        reg,
        value
    };

            byte cs = CalcChecksum(d);

            return new byte[]
            {
        0x55,
        cmd,
        reg,
        value,
        cs
            };
        }


        private byte CalcChecksum(byte[] buf)
        {
            ushort sum = 0;

            foreach (byte b in buf)
            {
                sum += b;
                if (sum > 0xFF)
                    sum -= 0xFF;
            }

            return (byte)~sum;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (serial != null && serial.IsOpen)
                serial.Close();

            base.OnFormClosing(e);
        }


        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex != 3)
                return;

            try
            {
                byte addr = Convert.ToByte(dgvRegisters.Rows[e.RowIndex].Cells[0].Value.ToString(), 16);

                byte value = Convert.ToByte(dgvRegisters.Rows[e.RowIndex].Cells[2].Value.ToString(), 16);

                WriteRegister(addr, value, force: true);

                MessageBox.Show($"Registro {addr:X2} scritto con valore {value:X2}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // Ferma il ciclo combinato Grafico/Misure e tutti i timer di catena.
        private void StopCombinedScan()
        {
            combinedScanEnabled = false;

            scanEnabled = false;
            scanEnabled_prec = false;

            misureScanEnabled = false;
            misureScanEnabled_prec = false;

            burst_interval.Enabled = false;
            burst_to_dump.Enabled = false;
            burst_decoding.Enabled = false;
            misura_to_result.Enabled = false;

            btnScanGrafico.BackColor = SystemColors.Control;
            btnScanGrafico.Text = "Attiva Scan";
        }

        // Pulsante unico: al primo click avvia il ciclo (parte dalla fase Grafico),
        // al click successivo lo ferma. Il ciclo alterna automaticamente
        // Grafico (reg 0x40 = 0x80) e Misure (reg 0x40 = 0x00) ogni 300ms,
        // svuotando il buffer seriale ad ogni cambio fase (vedi burst_interval_Elapsed).
        private void btnScanGrafico_Click(object sender, EventArgs e)
        {
            if (combinedScanEnabled)
            {
                StopCombinedScan();
                return;
            }

            if (offlineMode && (serial == null || !serial.IsOpen))
            {
                MessageBox.Show("Connettere prima la porta COM per avviare la scansione.");
                return;
            }

            if (!IndirizzoCorrisponde())
            {
                MessageBox.Show(
                    $"L'indirizzo digitato ({txtIndirizzo.Text}) non corrisponde a quello attualmente " +
                    $"in uso col dispositivo ({currentUartAddress}). Correggi l'indirizzo oppure premi " +
                    "\"Trasmetti a PGA\" per programmarlo sul PGA460 prima di avviare lo scan.");
                return;
            }

            combinedScanEnabled = true;

            burst_interval.Interval = SCAN_PHASE_INTERVAL_MS;
            burst_interval.AutoReset = true;

            // Avvia subito la prima fase (Grafico); il timer da 300ms scandirà
            try
            {
                // i cambi fase successivi.
                StartGraficoPhase();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[btnScanGrafico_Click] impossibile avviare lo scan: " + ex.Message);
                MessageBox.Show("Impossibile avviare la scansione: " + ex.Message);
                StopCombinedScan();
                return;
            }
            burst_interval.Enabled = true;

            btnScanGrafico.BackColor = Color.LimeGreen;
            btnScanGrafico.Text = "Ferma Scan";
        }

        private void SendBurst()
        {

            byte objectsToDetect = 1;

            byte cmd = BuildCommand(0x00);

            byte[] d =
            {
        cmd,objectsToDetect
    };

            byte cs = CalcChecksum(d);

            byte[] frame =
            {
        0x55,cmd,objectsToDetect,cs
    };
            lock (serialLock)
            {
                serial.Write(frame, 0, frame.Length);
            }

        }


        private byte[] ReadMeasurement()
        {
            byte cmd = BuildCommand(0x05);

            byte[] d =
            {
        cmd
    };

            byte cs = CalcChecksum(d);

            byte[] frame =
            {
        0x55, cmd, cs
    };

            lock (serialLock)
            {
                serial.DiscardInBuffer();

                serial.Write(frame, 0, frame.Length);

                Stopwatch sw = Stopwatch.StartNew();

                while (serial.BytesToRead < 6)
                {
                    if (sw.ElapsedMilliseconds > 100)
                    {
                        Debug.WriteLine("Timeout ReadMeasurement");
                        return null;
                    }

                    Thread.Sleep(1);
                }

                byte[] rx = new byte[6];

                serial.Read(rx, 0, 6);

                return rx;
              
            }
        }

        private void CheckObjectDetected(double distance, int size, int intensity)
        {
            if (!chkObjectDetect.Checked)
            {
                panelTrigger.BackColor = Color.LightGray;

                return;
            }

            double distanceSet = (double)numDistanceSet.Value;

            int sizeSet = (int)numWidthSet.Value;

            int intensitySet = (int)numAmplitudeSet.Value;

            //
            // Nessun oggetto rilevato
            //
            if (size == 255 && intensity == 255)
            {
                panelTrigger.BackColor = Color.LightGray;

                return;
            }

            bool distanceOk = (distanceSet == 0) || (distance <= distanceSet);

            bool sizeOk = (sizeSet == 0) || (size >= sizeSet);

            bool intensityOk = (intensitySet == 0) || (intensity >= intensitySet);

            bool detected = distanceOk && sizeOk && intensityOk;

            panelTrigger.BackColor = detected ? Color.Red : Color.LightGray;
        }

        private void DecodeMeasurement(byte[] rx)
        {
            Debug.WriteLine("MEASURE RX = " + BitConverter.ToString(rx));

            if (rx == null || rx.Length < 5)
                return;

            ushort tof = (ushort)((rx[1] << 8) | rx[2]);

            double distanceCm = tof * 0.01715;

            byte size = rx[3];

            byte intensity = rx[4];

            txtDistance.Text = distanceCm.ToString("F1");

            txtWidth.Text = size.ToString();

            txtAmplitude.Text = intensity.ToString();

            CheckObjectDetected(distanceCm, size, intensity);
        }


        private void SafeDrawDump(byte[] samples)
        {
            if (pictureBoxDump.IsHandleCreated && pictureBoxDump.InvokeRequired)
            {
                pictureBoxDump.BeginInvoke(new Action<byte[]>(SafeDrawDump), samples);
                return;
            }

            DrawDump(samples);
        }


        private void DrawDump(byte[] samples)
        {

            Bitmap bmp = new Bitmap(pictureBoxDump.Width, pictureBoxDump.Height);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);

                int w = bmp.Width;
                int h = bmp.Height;

                int left = 50;
                int right = 20;
                int top = 50;
                int bottom = 40;

                Rectangle area = new Rectangle(left, top, w - left - right, h - top - bottom);

                g.DrawRectangle(Pens.Black, area);

                //
                // asse tempo (0 ... 8.192 ms)
                //

                float recordTimeMs = 4.096f * (fields.P1_REC + 1);

                for (int i = 0; i <= 8; i++)
                {
                    float x = area.Left + i * area.Width / 8f;

                    g.DrawLine(Pens.LightGray, x, area.Top, x, area.Bottom);

                    double timeMs = i * (recordTimeMs / 8.0);

                    g.DrawString(timeMs.ToString("0.###"), Font, Brushes.Black, x - 15, area.Bottom + 5);

                    double distanceCm = timeMs * 1000.0 * 0.01715;

                    g.DrawString(distanceCm.ToString("0"), Font, Brushes.DarkBlue, x - 12, area.Bottom + 22);
                }

                g.DrawString("Time (ms)", Font, Brushes.Black, area.Left + area.Width / 2 - 30, area.Bottom + 25);

                //
                // asse Y
                //
                for (int v = 0; v <= 240; v += 20)
                {
                    float y = area.Bottom - v * area.Height / 240f;

                    g.DrawLine(Pens.Gainsboro, area.Left, y, area.Right, y);

                    g.DrawString(v.ToString(), Font, Brushes.Black, 5, y - 8);
                }

                g.DrawString("Data Path Value", Font, Brushes.Black, 5, 14);

                //
                // dump ADC
                //
                using (Pen p = new Pen(Color.Blue, 1))
                {
                    for (int i = 1; i < samples.Length; i++)
                    {
                        float x1 = area.Left + (i - 1) * area.Width / (float)(samples.Length - 1);

                        float x2 = area.Left + i * area.Width / (float)(samples.Length - 1);

                        float y1 = area.Bottom - samples[i - 1] * area.Height / 240f;

                        float y2 = area.Bottom - samples[i] * area.Height / 240f;

                        g.DrawLine(p, x1, y1, x2, y2);
                    }
                }

                if (refreshGraph || cachedThresholdPts == null || cachedGainPts == null)
                {
                    cachedThresholdPts = BuildThresholdCurve(area);
                    cachedGainPts = BuildTVGCurve(area);

                    refreshGraph = false;
                }

                List<PointF> thrPts = cachedThresholdPts;

                using (Pen p = new Pen(Color.Red, 2))
                {
                    for (int i = 1; i < thrPts.Count; i++)
                    {
                        g.DrawLine(p, thrPts[i - 1], thrPts[i]);
                    }

                    foreach (PointF pt in thrPts)
                    {
                        g.FillEllipse(Brushes.Red, pt.X - 3, pt.Y - 3, 6, 6);
                    }
                }

                List<PointF> gainPts = cachedGainPts;

                using (Pen p = new Pen(Color.Green, 2))
                {
                    for (int i = 1; i < gainPts.Count; i++)
                    {
                        g.DrawLine(p, gainPts[i - 1], gainPts[i]);
                    }

                    foreach (PointF pt in gainPts)
                    {
                        g.FillEllipse(Brushes.Green, pt.X - 3, pt.Y - 3, 6, 6);
                    }
                }
                //
                // legenda
                //
                g.DrawString("Blu = Echo Dump", Font, Brushes.Blue, area.Left + 120, 2);

                g.DrawString("Rosso = Trigger", Font, Brushes.Red, area.Left + 240, 2);

                g.DrawString("Verde = TVG Gain", Font, Brushes.Green, area.Left + 360, 2);
            }

            pictureBoxDump.Image = bmp;

        }



        private List<PointF> BuildThresholdCurve(Rectangle area)
        {
            List<PointF> pts = new List<PointF>();

            int[] T =
            {
           fields.TH_P1_T1,
           fields.TH_P1_T2,
           fields.TH_P1_T3,
           fields.TH_P1_T4,
           fields.TH_P1_T5,
           fields.TH_P1_T6,
           fields.TH_P1_T7,
           fields.TH_P1_T8,
           fields.TH_P1_T9,
           fields.TH_P1_T10,
           fields.TH_P1_T11,
           fields.TH_P1_T12
       };

            int[] L =
            {
           fields.TH_P1_L1,
           fields.TH_P1_L2,
           fields.TH_P1_L3,
           fields.TH_P1_L4,
           fields.TH_P1_L5,
           fields.TH_P1_L6,
           fields.TH_P1_L7,
           fields.TH_P1_L8,
           fields.TH_P1_L9,
           fields.TH_P1_L10,
           fields.TH_P1_L11,
           fields.TH_P1_L12
       };

            const float totalTimeUs = 8200f;   // fondo scala fisso

            float currentTimeUs = 0f;

            for (int i = 0; i < 12; i++)
            {

                // Tempo del segmento in microsecondi

                float segmentTimeUs = T[i] * 100f + 100f;

                // X1 = tempo attuale scalato

                float x1 = area.Left + (currentTimeUs / totalTimeUs) * area.Width;

                // Calcolo Y come nel tuo codice

                float maxValue = (i < 8) ? 31f : 255f;

                float y = area.Bottom - (L[i] * area.Height / maxValue); // /maxValue


                // CLIPPING X1

                if (x1 > area.Right)
                {
                    if (pts.Count > 0)
                    {
                        PointF prev = pts[pts.Count - 1];

                        float clippedY = prev.Y + (y - prev.Y) * ((area.Right - prev.X) / (x1 - prev.X));

                        pts.Add(new PointF(area.Right, clippedY));
                    }
                    break;
                }

                pts.Add(new PointF(x1, y));


                // Aggiorno il tempo per il punto successivo

                currentTimeUs += segmentTimeUs;

                // X2 = tempo aggiornato scalato

                float x2 = area.Left + (currentTimeUs / totalTimeUs) * area.Width;

                // CLIPPING X2

                if (x2 > area.Right)
                {
                    PointF prev = pts[pts.Count - 1];
                    float clippedY = prev.Y + (y - prev.Y) * ((area.Right - prev.X) / (x2 - prev.X));
                    pts.Add(new PointF(area.Right, clippedY));
                    break;
                }
                pts.Add(new PointF(x2, y));
            }
            return pts;
        }




        private List<PointF> BuildTVGCurve(Rectangle area)
        {
            List<PointF> pts = new List<PointF>();

            // Conversione registro -> tempo (µs)
            float[] tvgTimeUs =
            {
        100,
        200,
        300,
        400,
        600,
        800,
        1000,
        1200,
        1400,
        2000,
        2400,
        3200,
        4000,
        5200,
        6400,
        8000
    };

            // Durata totale del record (µs)
            float recordTimeUs = 4096f * (fields.P1_REC + 1);

            // Guadagni
            int[] G =
            {
        fields.TVG_G1,
        fields.TVG_G2,
        fields.TVG_G3,
        fields.TVG_G4,
        fields.TVG_G5
    };

            // Tempi convertiti
            float[] T =
            {
        tvgTimeUs[fields.TVG_T0],
        tvgTimeUs[fields.TVG_T1],
        tvgTimeUs[fields.TVG_T2],
        tvgTimeUs[fields.TVG_T3],
        tvgTimeUs[fields.TVG_T4],
        tvgTimeUs[fields.TVG_T5]
    };

            // Tempo assoluto del primo punto
            float currentTime = T[0];

            // Gain iniziale
            float initGain = fields.GAIN_INIT * 0.5f;

            const float maxGain = 32.0f;

            // Punto iniziale (tempo = 0)
            float x0 = area.Left;
            float y0 = area.Bottom - (fields.GAIN_INIT * area.Height / maxGain);

            pts.Add(new PointF(x0, y0));

            // Punto a TVG_T0 (stesso gain iniziale)
            float xStart = area.Left + (currentTime / recordTimeUs) * area.Width;

            pts.Add(new PointF(xStart, y0));


            //-------------------------
            // TVG_G1 ... TVG_G5
            //-------------------------

            for (int i = 0; i < G.Length; i++)
            {
                if (i > 0)
                    currentTime += T[i];

                if (currentTime > recordTimeUs)
                    currentTime = recordTimeUs;

                float x = area.Left + (currentTime / recordTimeUs) * area.Width;

                float y = area.Bottom - (G[i] * area.Height / maxGain);

                pts.Add(new PointF(x, y));

                if (currentTime >= recordTimeUs)
                    break;
            }

            //-------------------------
            // Ultimo tratto orizzontale
            //-------------------------

            if (pts.Count > 0)
            {
                PointF last = pts[pts.Count - 1];

                pts.Add(new PointF(area.Right, last.Y));
            }

            return pts;

    }
    }
}
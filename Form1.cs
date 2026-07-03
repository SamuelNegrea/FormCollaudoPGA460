using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
        //private Timer scanTimer = new Timer();
        //private System.Windows.Forms.Timer scanTimer = new System.Windows.Forms.Timer();
        private bool scanEnabled = false;
        private bool scanEnabled_prec = false;
        private bool misureScanEnabled = false;
        private bool misureScanEnabled_prec = false;
        private byte[] virtualRegs = new byte[256];
        private bool offlineMode = false;
        private DataGridView dgvFields;
        int var = 0;
        private readonly object serialLock = new object();
        private volatile bool refreshGraph = true;
        private List<PointF> cachedThresholdPts = null;
        private List<PointF> cachedGainPts = null;



        private void LoadDefaultRegisters()
        {
            WriteRegister(0x14, 0xAA);
            WriteRegister(0x15, 0x09);
            WriteRegister(0x16, 0xAA);
            WriteRegister(0x17, 0x3D);
            WriteRegister(0x18, 0xC9);
            WriteRegister(0x19, 0x00);
            WriteRegister(0x1A, 0x00);
            WriteRegister(0x1B, 0x4D);
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

            WriteRegister(0x5F, 0x88);
            WriteRegister(0x60, 0x88);
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
            //WriteRegister(0x2B, fields.EE_CRC);
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


        private static System.Timers.Timer burst_interval;
        private static System.Timers.Timer burst_to_dump;
        private static System.Timers.Timer burst_decoding;
        private static System.Timers.Timer misura_to_result;
        private static System.Timers.Timer misura_interval;

        public Form1()
        {
            InitializeComponent();

            numBurstInterval.Minimum = 10;
            numBurstInterval.Maximum = 100;
            numBurstInterval.Value = 50;

            numDistanceSet.Minimum = 0;
            numDistanceSet.Maximum = 10000;

            numWidthSet.Minimum = 0;
            numWidthSet.Maximum = 255;

            numAmplitudeSet.Minimum = 0;
            numAmplitudeSet.Maximum = 255;

            chkObjectDetect.Checked = true;

            LoadComPorts();

            btnConnect.Click += btnConnect_Click;


            InitGrid();
            LoadRegisters();

            InitFieldsGrid();
            VirtualRegistersToGrid();
            UpdateFieldsGrid();

            panelTrigger.BackColor = Color.LightGray;

            panelTrigger.BorderStyle = BorderStyle.FixedSingle;

            btnScanMisure.Click += btnScanMisure_Click;
            btnScanGrafico.Click += btnScanGrafico_Click;

            scanTimer.Interval = 1000;
            //scanTimer.Tick += ScanTimer_Tick;

            btnReadConfig.Click += btnReadConfig_Click;
            btnWriteConfig.Click += btnWriteConfig_Click;

            burst_to_dump = new System.Timers.Timer(50);      // intervallo in ms

            burst_to_dump.AutoReset = false;       // ripete automaticamente

            burst_to_dump.Elapsed += burst_to_dump_Elapsed;   // handler dell’evento



            burst_decoding = new System.Timers.Timer(200);

            burst_decoding.AutoReset = false;       // ripete automaticamente

            burst_decoding.Elapsed += burst_decoding_Elapsed;  // handler dell’evento

            burst_interval = new System.Timers.Timer(1000);      // intervallo in ms

            burst_interval.Elapsed += burst_interval_Elapsed;

            burst_interval.AutoReset = true;       // ripete automaticamente

            // --- Sezione MISURE: timer indipendenti, non toccano quelli sopra ---
            misura_to_result = new System.Timers.Timer(50);      // intervallo in ms

            misura_to_result.AutoReset = false;

            misura_to_result.Elapsed += misura_to_result_Elapsed;

            misura_interval = new System.Timers.Timer(1000);      // intervallo in ms

            misura_interval.Elapsed += misura_interval_Elapsed;
            misura_interval.AutoReset = true;

        }



        private void burst_interval_Elapsed(object sender, ElapsedEventArgs e)

        {
            //System.Diagnostics.Debug.WriteLine(var++);
            SendBurst();

            //burst_to_dump.Enabled = true;         // avvia il timer
            burst_to_dump.Interval = 50;
            burst_to_dump.Start();


        }


        private void misura_interval_Elapsed(object sender, ElapsedEventArgs e)
        {
            SendBurst();

            misura_to_result.Interval = 50;
            misura_to_result.Start();
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
        }

        private void burst_to_dump_Elapsed(object sender, ElapsedEventArgs e)

        {

            //burst_decoding.Enabled = true;         // avvia il timer

            burst_to_dump.Enabled = false;

            byte[] d = { 0x07 };

            byte cs = CalcChecksum(d);

            byte[] frame =
            {
        0x55,
        0x07,
        cs
    };

            try
            {
                lock (serialLock)
                {
                    serial.Write(frame, 0, frame.Length);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            burst_decoding.Interval = 150;
            burst_decoding.Start();

            /*int n = serial.BytesToRead;

            byte[] rx = new byte[n];

            serial.Read(rx, 0, n);*/

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

                        Debug.WriteLine($"[ReadEchoDump] received chunk {n} bytes, total {received}/{PACKET_SIZE}");
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
            }

            Debug.WriteLine($"[ReadEchoDump] total received {received} bytes");

            return rx;
        }

        private void burst_decoding_Elapsed(object sender, ElapsedEventArgs e)

        {
            burst_decoding.Enabled = false;
            Debug.WriteLine("messaggio ogni secondo" + var++);


            byte[] dump = ReadEchoDump();

            if (dump == null || dump.Length < 130)
            {
                Debug.WriteLine("[burst_decoding] Dump non valido o incompleto");
                MessageBox.Show("Dump non valido");
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

                Debug.WriteLine($"[DUMP] samples len={samples.Length} first={samples[0]} mid={samples[samples.Length/2]} last={samples[samples.Length-1]} min={min} max={max}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[DUMP] diagnostic error: " + ex.Message);
            }

            DrawDump(samples);

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

        private void btnConnect_Click(object sender, EventArgs e)
        {
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

                LoadDefaultRegisters();


                bool prevOfflineMode = offlineMode;
                offlineMode = true;
                DecodeRegisters14To6E();
                offlineMode = prevOfflineMode;

                UpdateFieldsGrid();
                refreshGraph = true;

                MessageBox.Show($"Connesso a {cmbCom.Text}");
            }
            catch (Exception ex)
            {
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

                MessageBox.Show(offlineMode ? "Decode completato" : "Configurazione letta dal PGA460");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnWriteConfig_Click(object sender, EventArgs e)
        {
            try
            {
                ReadFieldsGrid();
                EncodeRegisters14To6E();

                refreshGraph = true;

                if (offlineMode)
                    VirtualRegistersToGrid();

                MessageBox.Show(offlineMode ? "Encode completato" : "Configurazione scritta nel PGA460");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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
                serial.DiscardOutBuffer();

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


        private void WriteRegister(byte addr, byte value)
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

            byte[] frame = BuildWrite(addr, value);

            // Perform serial write under lock and update virtualRegs only after successful write
            lock (serialLock)
            {
                try
                {
                    serial.Write(frame, 0, frame.Length);
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

            dgvFields.Location = new Point(599, 15);
            dgvFields.Size = new Size(729, 185);
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
                foreach (
                    DataGridViewRow row in dgvRegisters.Rows)
                {
                    byte addr = Convert.ToByte(row.Cells[0].Value.ToString(), 16);

                    byte value = ReadRegister(addr);

                    row.Cells[2].Value = value.ToString("X2");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private void btnScrivi_Click(object sender, EventArgs e)
        {
            if (serial == null || !serial.IsOpen)
            {
                MessageBox.Show("Connettere prima la porta COM");
                return;
            }

            try
            {
                byte addr = Convert.ToByte(txtIndirizzo.Text, 16);
                byte val = Convert.ToByte(txtValore.Text, 16);

                byte[] frame = BuildWrite(addr, val);

                serial.Write(frame, 0, frame.Length);

                MessageBox.Show("TX: " + BitConverter.ToString(frame));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Errore SCRIVI: " + ex.Message);
            }
        }

        private void btnLeggi_Click(object sender, EventArgs e)
        {
            if (serial == null || !serial.IsOpen)
            {
                MessageBox.Show("Connettere prima la porta COM");
                return;
            }

            try
            {
                byte addr = Convert.ToByte(txtIndirizzo.Text, 16);

                byte[] frame = BuildRead(addr);

                MessageBox.Show("TX = " + BitConverter.ToString(frame));

                lock (serialLock)
                {
                    serial.DiscardInBuffer();

                    serial.Write(frame, 0, frame.Length);

                    Stopwatch sw = Stopwatch.StartNew();

                    while (serial.BytesToRead < 3)
                    {
                        if (sw.ElapsedMilliseconds > 100)
                        {
                            MessageBox.Show("Timeout risposta PGA460");
                            return;
                        }

                        Thread.Sleep(1);
                    }

                    byte[] rx = new byte[3];

                    serial.Read(rx, 0, 3);

                    MessageBox.Show("RX: " + BitConverter.ToString(rx));

                    txtValore.Text = rx[1].ToString("X2");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Errore LEGGI: " + ex.Message);
            }
        }

        private byte[] BuildRead(byte addr)
        {
            byte[] d = { 0x09, addr };
            byte cs = CalcChecksum(d);

            return new byte[] { 0x55, 0x09, addr, cs };
        }

        private byte[] BuildWrite(byte addr, byte val)
        {
            byte[] d = { 0x0A, addr, val };
            byte cs = CalcChecksum(d);

            return new byte[] { 0x55, 0x0A, addr, val, cs };
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

                WriteRegister(addr, value);

                MessageBox.Show($"Registro {addr:X2} scritto con valore {value:X2}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnScanMisure_Click(object sender, EventArgs e)
        {
            misureScanEnabled = !misureScanEnabled;

            if (misureScanEnabled)
            {
                // Set point di modalità (reg 0x40 = 0x00) scritto una sola
                // volta, all'avvio dello scan misure.
                WriteRegister(0x40, 0x00);
            }

            if ((misureScanEnabled == true) && (misureScanEnabled_prec == false))
            {
                misureScanEnabled_prec = true;

                misura_interval.Enabled = true;         // avvia il timer
            }

            if ((misureScanEnabled == false) && (misureScanEnabled_prec == true))
            {
                misura_interval.Enabled = false;
                misureScanEnabled_prec = false;
            }

            btnScanMisure.BackColor = misureScanEnabled ? Color.LimeGreen : SystemColors.Control;
            btnScanMisure.Text = misureScanEnabled ? "Ferma Scan Misure" : "Attiva Scan Misure";
        }

        private void btnScanGrafico_Click(object sender, EventArgs e)
        {
            scanEnabled = !scanEnabled;

            if (scanEnabled)
            {
                // Set point di modalità (reg 0x40 = 0x80, DATADUMP_EN) scritto
                // una sola volta, all'avvio dello scan grafico.
                WriteRegister(0x40, 0x80);
            }

            if ((scanEnabled == true) && (scanEnabled_prec == false))
            {
                scanEnabled_prec = true;

                burst_interval.Enabled = true;         // avvia il timer
            }

            if ((scanEnabled == false) && (scanEnabled_prec == true))
            {
                burst_interval.Enabled = false;
                scanEnabled_prec = false;
            }

            btnScanGrafico.BackColor = scanEnabled ? Color.LimeGreen : SystemColors.Control;
            btnScanGrafico.Text = scanEnabled ? "Ferma Scan Grafico" : "Attiva Scan Grafico";
        }

        private void SendBurst()
        {

            byte objectsToDetect = 1;

            byte[] d =
            {
        0x00,objectsToDetect
    };

            byte cs = CalcChecksum(d);

            byte[] frame =
            {
        0x55,0x00,objectsToDetect,cs
    };
            lock (serialLock)
            {
                serial.Write(frame, 0, frame.Length);
            }

        }


        private byte[] ReadMeasurement()
        {
            byte[] d =
            {
        0x05
    };

            byte cs = CalcChecksum(d);

            byte[] frame =
            {
        0x55, 0x05, cs
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

            bool distanceOk = (distanceSet == 0) || (distance >= distanceSet);

            bool sizeOk = (sizeSet == 0) || (size >= sizeSet);

            bool intensityOk = (intensitySet == 0) || (intensity >= intensitySet);

            bool detected = distanceOk && sizeOk && intensityOk;

            panelTrigger.BackColor = detected ? Color.LimeGreen : Color.LightGray;
        }

        private void DecodeMeasurement(byte[] rx)
        {

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

        private void NumBurstInterval_ValueChanged(object sender, EventArgs e)
        {
            scanTimer.Interval = (int)numBurstInterval.Value;
        }


        private void DrawDump(byte[] samples)
        {
            //DecodeRegisters14To6E();

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

            float totalTime = T.Sum();

            if (totalTime <= 0)
                totalTime = 1;

            float currentTime = 0;

            for (int i = 0; i < 12; i++)
            {
                float x1 = area.Left + (currentTime / totalTime) * area.Width;

                float maxValue = (i < 8) ? 31f : 255f;

                float y = area.Bottom - (L[i] * area.Height / maxValue);

                pts.Add(new PointF(x1, y));

                currentTime += T[i];

                float x2 = area.Left + (currentTime / totalTime) * area.Width;

                pts.Add(new PointF(x2, y));
            }

            return pts;
        }

        private float GetAfeGainDb()
        {
            switch (fields.AFE_GAIN_RNG)
            {
                case 0: return 32f;
                case 1: return 38f;
                case 2: return 44f;
                case 3: return 50f;
                default: return 32f;
            }
        }

        private List<PointF> BuildTVGCurve(Rectangle area)
        {
            List<PointF> pts = new List<PointF>();

            int[] T =
            {
        fields.TVG_T0,
        fields.TVG_T1,
        fields.TVG_T2,
        fields.TVG_T3,
        fields.TVG_T4,
        fields.TVG_T5
    };

            int[] G =
            {
        fields.TVG_G1,
        fields.TVG_G2,
        fields.TVG_G3,
        fields.TVG_G4,
        fields.TVG_G5
    };

            float totalTime = T.Sum();

            if (totalTime <= 0)
                totalTime = 1;

            float currentTime = 0;

            float afeGainDb = GetAfeGainDb();

            const float maxGainDb = 82f;

            for (int i = 0; i < G.Length; i++)
            {

                float gainDb = 0.5f * (G[i] + 1) + afeGainDb;

                float x1 = area.Left + (currentTime / totalTime) * area.Width;

                //float y =area.Bottom -((G[i] * 240f / 63f) *area.Height / 240f);
                float y = area.Bottom - (gainDb * area.Height / maxGainDb);

                //float y = area.Bottom - (G[i] * area.Height / 63f);

                pts.Add(new PointF(x1, y));

                if (i < T.Length)
                    currentTime += T[i];

                float x2 = area.Left + (currentTime / totalTime) * area.Width;

                pts.Add(new PointF(x2, y));
            }

            return pts;
        }
    }
}

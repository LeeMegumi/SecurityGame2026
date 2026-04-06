using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using SCIP_library;

/// <summary>
/// Hokuyo UST-10LX 連線管理 (Ethernet)
/// 掛在場景中任一 GameObject 上，負責在背景執行緒持續讀取測距數據
/// </summary>
public class HokuyoManager : MonoBehaviour
{
    [Header("─── Ethernet 連線設定 ───")]
    public string ipAddress   = "192.168.0.10";
    public int    port        = 10940;

    [Header("─── 掃描範圍 (UST-10LX: 0 ~ 1080) ───")]
    [Tooltip("對應牆面左緣的 step")]
    public int startStep = 100;
    [Tooltip("對應牆面右緣的 step")]
    public int endStep   = 980;

    // ── 公開狀態 ──────────────────────────────────────
    public bool IsConnected  { get; private set; } = false;
    public long TimeStamp    { get; private set; } = 0;

    // ── 執行緒安全的距離資料 ──────────────────────────
    private List<long> _distances = new List<long>();
    private readonly object _lock = new object();

    public List<long> GetDistances()
    {
        lock (_lock) { return new List<long>(_distances); }
    }

    // ── 私有成員 ──────────────────────────────────────
    private TcpClient     _client;
    private NetworkStream _stream;
    private Thread        _readThread;
    private volatile bool _isRunning = false;

    void Start() => Connect();

    // ── 連線 ──────────────────────────────────────────
    public void Connect()
    {
        try
        {
            _client = new TcpClient();
            _client.Connect(ipAddress, port);
            _stream = _client.GetStream();

            Write(SCIP_Writer.SCIP2());
            ReadLine();                       // 略過 echo back

            // MD: 全範圍測距，座標篩選在 Detector 端做
            Write(SCIP_Writer.MD(0, 1080));
            ReadLine();                       // 略過 echo back

            IsConnected = true;
            _isRunning  = true;

            _readThread = new Thread(ReadLoop) { IsBackground = true };
            _readThread.Start();

            Debug.Log($"[Hokuyo] 已連線 → {ipAddress}:{port}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Hokuyo] 連線失敗：{ex.Message}");
        }
    }

    // ── 背景讀取迴圈 ──────────────────────────────────
    private void ReadLoop()
    {
        var tempDist = new List<long>();
        long tempTs  = 0;

        while (_isRunning)
        {
            try
            {
                string data = ReadLine();
                if (data == null) break;

                tempDist.Clear();
                if (SCIP_Reader.MD(data, ref tempTs, ref tempDist) && tempDist.Count > 0)
                {
                    lock (_lock)
                    {
                        _distances = new List<long>(tempDist);
                        TimeStamp  = tempTs;
                    }
                }
            }
            catch (Exception ex)
            {
                if (_isRunning)
                    Debug.LogWarning($"[Hokuyo] 讀取錯誤：{ex.Message}");
                break;
            }
        }
    }

    // ── SCIP 協定：讀到雙換行 ─────────────────────────
    private string ReadLine()
    {
        if (_stream == null || !_stream.CanRead) return null;
        var sb    = new StringBuilder();
        bool isNL = false, isNL2 = false;
        do
        {
            int b = _stream.ReadByte();
            if (b == -1) return null;
            char c = (char)b;
            if (c == '\n') { if (isNL) isNL2 = true; else isNL = true; }
            else isNL = false;
            sb.Append(c);
        } while (!isNL2);
        return sb.ToString();
    }

    private void Write(string data)
    {
        if (_stream != null && _stream.CanWrite)
        {
            byte[] buf = Encoding.ASCII.GetBytes(data);
            _stream.Write(buf, 0, buf.Length);
        }
    }

    // ── 離開時停止感測器 ──────────────────────────────
    void OnDestroy()
    {
        _isRunning = false;
        try { Write(SCIP_Writer.QT()); } catch { }
        try { _stream?.Close();        } catch { }
        try { _client?.Close();        } catch { }
        _readThread?.Join(500);
        Debug.Log("[Hokuyo] 已中斷連線");
    }
}

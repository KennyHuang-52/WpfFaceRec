 WpfFaceRec

WPF 實作的即時人臉辨識 + 自動白平衡調整工具，使用 Emgu CV 開發。

-  即時攝影機影像擷取（透過 `VideoCapture`）
-  人臉偵測（使用 Haar cascade）
-  自動白平衡計算 + 滑桿調整強度
-  WPF UI 整合，可視化顯示處理結果
- .NET Framework 4.8
- Emgu CV 4.8
- WPF + XAML
- Haar cascade: `haarcascade_frontalface_default.xml`

 執行
1. 安裝 Emgu.CV
2. 專案目錄內放入 `haarcascade_frontalface_default.xml`
3. 執行程式即可使用

 檔案

- `MainWindow.xaml.cs`：主邏輯（攝影機、人臉、白平衡）
- `BitmapSourceConvert`：將 Emgu 的影像轉為 WPF 顯示
- `AWBGainSlider`：白平衡強度控制（UI 中定義）

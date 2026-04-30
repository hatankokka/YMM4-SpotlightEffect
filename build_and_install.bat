@echo off
echo ========================================
echo  SpotlightEffect ビルド & インストール
echo ========================================
echo.

dotnet build SpotlightEffect.csproj -c Release

if errorlevel 1 (
    echo.
    echo [失敗] ビルドエラーが発生しました。
    echo 上のエラーメッセージを確認してください。
    pause
    exit /b 1
)

echo.
echo [成功] ビルド完了！YMM4を再起動してください。
echo エフェクトは「映像エフェクト追加 → 合成 → スポットライト」から使えます。
pause

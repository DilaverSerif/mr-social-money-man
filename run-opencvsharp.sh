#!/bin/bash

# 🔧 Yol ayarları
PROJECT_ROOT="/home/bladon/Documents/Workspace/mr-social-money-man"
SOURCE_SO="/home/bladon/Documents/Workspace/opencvsharp/src/OpenCvSharpExtern/build/libOpenCvSharpExtern.so"
TARGET_NATIVE="$PROJECT_ROOT/bin/Debug/net9.0/runtimes/linux-x64/native"

# 📁 Hedef klasörü oluştur
mkdir -p "$TARGET_NATIVE"

# 📦 Dosyayı kopyala
if [ -f "$SOURCE_SO" ]; then
    echo "✅ libOpenCvSharpExtern.so bulundu, kopyalanıyor..."
    cp "$SOURCE_SO" "$TARGET_NATIVE/"
else
    echo "❌ HATA: Derlenmiş libOpenCvSharpExtern.so bulunamadı:"
    echo "   $SOURCE_SO"
    exit 1
fi

# 🌐 Ortam değişkenini ayarla
export LD_LIBRARY_PATH="$TARGET_NATIVE:$LD_LIBRARY_PATH"

# 🚀 Projeyi çalıştır
cd "$PROJECT_ROOT"
dotnet run

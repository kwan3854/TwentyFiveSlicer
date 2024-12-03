using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace TwentyFiveSlicer.Runtime
{
    public static class TfsHashGenerator
    {
        public static string GenerateUniqueSpriteHash(Sprite sprite)
        {
            string textureName = sprite.texture.name;
            Rect rect = sprite.rect;

            // 픽셀 데이터로 MD5 해시 생성
            Color[] pixels = sprite.texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
            StringBuilder sb = new StringBuilder();
            foreach (var pixel in pixels)
            {
                sb.Append(pixel.r).Append(pixel.g).Append(pixel.b).Append(pixel.a);
            }
            byte[] hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
            StringBuilder hashString = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                hashString.Append(b.ToString("X2"));
            }
            return $"{textureName}_{rect.width}_{rect.height}_{hashString}";
        }
    }
}
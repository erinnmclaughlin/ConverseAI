using System.Text;

namespace ConverseAI;

public static class FileUtilities
{
    public static void SaveAsTextFile(string fileName, string content)
    {
        File.WriteAllText(fileName, content);
    }
    
    public static void SaveAsWaveFile(string fileName, string base64Audio)
    {
        var pcmData = Convert.FromBase64String(base64Audio);
        var wavHeader = CreateWavHeader(pcmData.Length, 24000, 1, 16);
        using var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);
        fs.Write(wavHeader);    // Write WAV header
        fs.Write(pcmData);      // Write PCM data
    }
    
    private static byte[] CreateWavHeader(int pcmDataLength, int sampleRate, short channels, short bitsPerSample)
    {
        var byteRate = sampleRate * channels * (bitsPerSample / 8);
        var blockAlign = (short)(channels * (bitsPerSample / 8));
        var subchunk2Size = pcmDataLength;
        var chunkSize = 36 + subchunk2Size;

        using var ms = new MemoryStream(44);
        using var writer = new BinaryWriter(ms);
        
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));  // ChunkID
        writer.Write(chunkSize);                        // ChunkSize
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));  // Format

        writer.Write(Encoding.ASCII.GetBytes("fmt "));  // Subchunk1ID
        writer.Write(16);                               // Subchunk1Size
        writer.Write((short)1);                         // AudioFormat (1 = PCM)
        writer.Write(channels);                         // NumChannels
        writer.Write(sampleRate);                       // SampleRate
        writer.Write(byteRate);                         // ByteRate
        writer.Write(blockAlign);                       // BlockAlign
        writer.Write(bitsPerSample);                    // BitsPerSample

        writer.Write(Encoding.ASCII.GetBytes("data"));  // Subchunk2ID
        writer.Write(subchunk2Size);                    // Subchunk2Size

        return ms.ToArray();
    }
}
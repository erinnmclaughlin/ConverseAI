using System.Text;

namespace ConverseAI;

public static class FileUtilities
{
    private const int SampleRate = 24100; // actually 24000, but they are so boring lmao, speeding it up a tad
    
    public static void InitializeWavFile(this FileStream fileStream)
    {
        var placeholderHeader = CreateWavHeader(0, SampleRate, 1, 16);
        fileStream.Write(placeholderHeader);
    }
    
    public static void FinalizeWavFile(this FileStream fileStream)
    {
        // Calculate the final sizes.
        var pcmDataLength = (int)(fileStream.Length - 44); // Exclude header size.
        var finalHeader = CreateWavHeader(pcmDataLength, SampleRate, 1, 16);

        // Write the correct header at the beginning of the file.
        fileStream.Seek(0, SeekOrigin.Begin);
        fileStream.Write(finalHeader);
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
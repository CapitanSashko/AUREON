using NAudio.Wave;
using System;

namespace MP3
{
    /// <summary>
    /// Дополнительные звуковые эффекты AUREON:
    /// Bass Boost, Stereo Virtualizer и Sub-bass.
    /// Ставится после EqualizerSampleProvider и до BalanceSampleProvider.
    /// </summary>
    public sealed class AudioEffectsSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider source;
        private readonly int channels;
        private readonly int sampleRate;
        private readonly object settingsLock = new();

        private float targetBassBoostDb;
        private float targetSubBassDb;
        private float targetVirtualizer;

        // Текущие значения плавно приближаются к заданным, чтобы крутилки
        // не вызывали резких скачков коэффициентов фильтра.
        private float currentBassBoostDb;
        private float currentSubBassDb;
        private float currentVirtualizer;

        private readonly float[] bassZ1;
        private readonly float[] bassZ2;
        private readonly float[] subZ1;
        private readonly float[] subZ2;

        private volatile bool enabled = true;

        public AudioEffectsSampleProvider(ISampleProvider source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            channels = Math.Max(1, source.WaveFormat.Channels);
            sampleRate = source.WaveFormat.SampleRate;

            bassZ1 = new float[channels];
            bassZ2 = new float[channels];
            subZ1 = new float[channels];
            subZ2 = new float[channels];
        }

        public WaveFormat WaveFormat => source.WaveFormat;

        public bool Enabled
        {
            get => enabled;
            set => enabled = value;
        }

        /// <summary>Усиление низких частот, 0–12 дБ.</summary>
        public float BassBoostDb
        {
            get { lock (settingsLock) return targetBassBoostDb; }
            set
            {
                lock (settingsLock)
                    targetBassBoostDb = Math.Clamp(value, 0f, 12f);
            }
        }

        /// <summary>Расширение стереобазы, 0–100 процентов.</summary>
        public float Virtualizer
        {
            get { lock (settingsLock) return targetVirtualizer; }
            set
            {
                lock (settingsLock)
                    targetVirtualizer = Math.Clamp(value, 0f, 100f);
            }
        }

        /// <summary>Усиление самого нижнего баса, 0–12 дБ.</summary>
        public float SubBassDb
        {
            get { lock (settingsLock) return targetSubBassDb; }
            set
            {
                lock (settingsLock)
                    targetSubBassDb = Math.Clamp(value, 0f, 12f);
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int read = source.Read(buffer, offset, count);
            if (read <= 0 || !enabled)
                return read;

            float bassTarget;
            float subTarget;
            float virtualTarget;

            lock (settingsLock)
            {
                bassTarget = targetBassBoostDb;
                subTarget = targetSubBassDb;
                virtualTarget = targetVirtualizer;
            }

            // Плавное изменение параметров между аудиоблоками.
            currentBassBoostDb = MoveTowards(currentBassBoostDb, bassTarget, 0.35f);
            currentSubBassDb = MoveTowards(currentSubBassDb, subTarget, 0.35f);
            currentVirtualizer = MoveTowards(currentVirtualizer, virtualTarget, 4f);

            // Bass Boost — широкая низкая полка около 110 Гц.
            // Sub-bass — отдельная полка около 45 Гц.
            FilterCoefficients bass = CreateLowShelf(sampleRate, 110f, currentBassBoostDb);
            FilterCoefficients sub = CreateLowShelf(sampleRate, 45f, currentSubBassDb);

            if (channels == 2)
            {
                float width = 1f + (currentVirtualizer / 100f) * 0.75f;

                for (int i = 0; i + 1 < read; i += 2)
                {
                    float left = ProcessSample(buffer[offset + i], 0, bass, sub);
                    float right = ProcessSample(buffer[offset + i + 1], 1, bass, sub);

                    // Mid/Side widening: центр остаётся стабильным,
                    // боковая составляющая расширяется до 1.75×.
                    if (currentVirtualizer > 0.01f)
                    {
                        float mid = (left + right) * 0.5f;
                        float side = (left - right) * 0.5f * width;
                        left = mid + side;
                        right = mid - side;
                    }

                    buffer[offset + i] = left;
                    buffer[offset + i + 1] = right;
                }
            }
            else
            {
                // Для моно Bass Boost и Sub-bass работают,
                // а виртуализатор не меняет сигнал.
                for (int i = 0; i < read; i++)
                    buffer[offset + i] = ProcessSample(buffer[offset + i], 0, bass, sub);
            }

            return read;
        }

        private float ProcessSample(
            float input, int channel,
            FilterCoefficients bass, FilterCoefficients sub)
        {
            float value = ProcessBiquad(input, channel, bass, bassZ1, bassZ2);
            return ProcessBiquad(value, channel, sub, subZ1, subZ2);
        }

        private static float ProcessBiquad(
            float input, int channel, FilterCoefficients c,
            float[] z1, float[] z2)
        {
            float output = c.B0 * input + z1[channel];
            z1[channel] = c.B1 * input - c.A1 * output + z2[channel];
            z2[channel] = c.B2 * input - c.A2 * output;
            return output;
        }

        private static float MoveTowards(float current, float target, float step)
        {
            float difference = target - current;
            if (Math.Abs(difference) <= step)
                return target;

            return current + Math.Sign(difference) * step;
        }

        private readonly struct FilterCoefficients
        {
            public readonly float B0, B1, B2, A1, A2;

            public FilterCoefficients(float b0, float b1, float b2, float a1, float a2)
            {
                B0 = b0;
                B1 = b1;
                B2 = b2;
                A1 = a1;
                A2 = a2;
            }

            public static FilterCoefficients Identity =>
                new(1f, 0f, 0f, 0f, 0f);
        }

        // RBJ low-shelf biquad, slope S = 1.
        private static FilterCoefficients CreateLowShelf(int sampleRate, float frequency, float gainDb)
        {
            if (Math.Abs(gainDb) < 0.001f || sampleRate <= 0)
                return FilterCoefficients.Identity;

            frequency = Math.Clamp(frequency, 10f, sampleRate * 0.40f);

            double a = Math.Pow(10.0, gainDb / 40.0);
            double w0 = 2.0 * Math.PI * frequency / sampleRate;
            double cos = Math.Cos(w0);
            double sin = Math.Sin(w0);
            const double slope = 1.0;

            double alpha = sin / 2.0 * Math.Sqrt(
                (a + 1.0 / a) * (1.0 / slope - 1.0) + 2.0);
            double beta = 2.0 * Math.Sqrt(a) * alpha;

            double b0 = a * ((a + 1.0) - (a - 1.0) * cos + beta);
            double b1 = 2.0 * a * ((a - 1.0) - (a + 1.0) * cos);
            double b2 = a * ((a + 1.0) - (a - 1.0) * cos - beta);
            double a0 = (a + 1.0) + (a - 1.0) * cos + beta;
            double a1 = -2.0 * ((a - 1.0) + (a + 1.0) * cos);
            double a2 = (a + 1.0) + (a - 1.0) * cos - beta;

            if (Math.Abs(a0) < 1e-12)
                return FilterCoefficients.Identity;

            return new FilterCoefficients(
                (float)(b0 / a0),
                (float)(b1 / a0),
                (float)(b2 / a0),
                (float)(a1 / a0),
                (float)(a2 / a0));
        }
    }

    /// <summary>
    /// Master gain stage. Placed after SpectrumSampleProvider so the visualizer
    /// remains independent of the master volume. Supports up to 150 percent.
    /// </summary>
    public sealed class MasterVolumeSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider source;
        private volatile float volume = 1f;

        public MasterVolumeSampleProvider(ISampleProvider source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public WaveFormat WaveFormat => source.WaveFormat;

        public float Volume
        {
            get => volume;
            set => volume = Math.Clamp(value, 0f, 1.5f);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int read = source.Read(buffer, offset, count);
            if (read <= 0) return read;

            float gain = volume;
            if (Math.Abs(gain - 1f) < 0.0001f) return read;
            if (gain <= 0f)
            {
                Array.Clear(buffer, offset, read);
                return read;
            }

            int end = offset + read;
            for (int i = offset; i < end; i++)
                buffer[i] *= gain;

            return read;
        }
    }

}

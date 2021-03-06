
using System;

/// <summary>
///   Generates samples with minimum distance
/// </summary>
/// <remarks>
///   This algorithm is a modification of http://extremelearning.com.au/isotropic-blue-noise-point-sets/
///   which can generate infinite amount of samples without tiling
///   and can calculate points at individual cells
///   without calculating neighbors.
///   Requires no storage or initial computations.
/// </remarks>
public readonly struct Pach2 : ISampler
{
    /// Repeatable random number generator
    private readonly Squirrel3 Noise;

    /// the cell size will be 2 ^ bits
    private readonly int Bits;

    /// <summary>
    ///   Generates samples with minimum distance
    /// </summary>
    /// <param name="seed">
    ///   Seed for random number generator
    ///   Different seeds produce different samples
    /// </param>
    /// <param name="bits">
    ///   Number of bits for the cell size
    /// </param>

    public Pach2(int bits, uint seed)
    {
        this.Noise = new Squirrel3(seed);
        this.Bits = bits;
    }

    // generate a possible sample at a corner
    // the result is a random x,y number between 0 and 2^bits
    private Sample GenerateCandidate(int row, int col)
    {
        uint cellSize = 1u << Bits;
        uint mask = cellSize - 1;

        uint rnd = Noise[row, col];
        int xr = (int)(rnd & mask);
        rnd >>= Bits;
        int yr = (int)(rnd & mask);
        rnd >>= Bits;

        // plus one so that this is not considered an invalid sample
        return new Sample { X = xr, Y = yr, Value = rnd + 1 };
    }

    /// <summary>
    /// Linear interpolation between a0 and a1
    /// by w/cellSize
    /// </summary>
    public int Lerp(int a0, int a1, int w)
    {
        int cellSize = 1 << Bits;
        // ((a1 - a0) * w + a0;
        return ((a1 - a0) * w + a0 * cellSize + (cellSize >> 1)) >> Bits;
    }
    
    /// <summary>
    ///   Generates a sample at the cell containing (x,y)
    /// </summary>
    public Sample this[int x, int y] 
    {
        get
        {
            int col = x >> Bits;
            int row = y >> Bits;

            // this is the bottom left origin of the cell where (x,y) is
            int x0 = col << Bits;
            int y0 = row << Bits;

            Sample sample;

            // there are 4 cases:
            if ((row & 1) == 0 && (col & 1) == 0)
            {
                // even row, even column
                // these cells are fully randomized, just get a sample and return it
                sample = GenerateCandidate(col, row);
            }
            else if ((row & 1) == 0 && (col & 1) == 1)
            {
                // even row, odd column
                sample = GetEvenRowSample(col, row);
                // sample.Value = 0;
            }
            else if ((row & 1) == 1 && (col & 1) == 0)
            {
                // odd row, even column
                sample = GetEvenColSample(col, row);
                // sample.Value = 0;
            }
            else
            {
                // odd row, odd column
                sample = GetEvenRowEvenColSample(col, row);
                // sample.Value = 0;
            }
            sample.X += x0;
            sample.Y += y0;
            return sample;

            // row is even, col is odd
            // row is odd, col is even
            // row is even, col is even
        }
    }

    private Sample GetEvenRowSample(int col, int row)
    {
        Sample s0 = GenerateCandidate(col - 1, row);
        Sample s1 = GenerateCandidate(col + 1, row);
        return GetRowAdjustedSample(col, row, s0, s1);
    }

    private Sample GetRowAdjustedSample(int col, int row, Sample s0, Sample s1)
    {
        int cellSize = 1 << Bits;
        int halfCell = cellSize >> 1;
        int mask = cellSize - 1;

        // every EE cell will have either horizontal or vertical orientation
        // That means that only one of s0 and s1 will be black
        // this variable determines which one it is
        bool leftIsHorizontal = ((row ^ col) & 2) == 0;

        uint whiteValue = leftIsHorizontal ? s0.Value : s1.Value;
        uint blackValue = leftIsHorizontal ? s1.Value : s0.Value;

        // get the y value from the black one one:
        int y = leftIsHorizontal ? (int)blackValue & mask : ((int)blackValue >> Bits) & mask;


        // get the y values from the black one:
        int x1 = (int)(whiteValue & mask);
        int x2 = (int)((whiteValue >> Bits) & mask);

        int x = leftIsHorizontal ? Math.Max(x1, x2) : Math.Min(x1, x2);

        bool valid = x >= s0.X && x <= s1.X;

        return new Sample
        {
            X = x,
            Y = y,
            Value = valid ? 1u : 0u
        };
    }

    private Sample GetEvenColSample(int col, int row)
    {
        Sample s0 = GenerateCandidate(col, row - 1);
        Sample s1 = GenerateCandidate(col, row + 1);
        return GetColAdjustedSample(col, row, s0, s1);
    }

    // s0 is the sample at the previous column
    // s1 is the sample at the next column
    // both of them would be at the same row
    private Sample GetColAdjustedSample(int col, int row, Sample s0, Sample s1)
    {
        int cellSize = 1 << Bits;
        int halfCell = cellSize >> 1;
        int mask = cellSize - 1;

        // every even cell will be colored with white or black and will alternate
        // That means that only one of s0 and s1 will be black
        // this variable determines which one it is
        bool topIsHorizontal = ((row ^ col) & 2) == 0;

        // get the x value from the white one:
        int x = topIsHorizontal ? (int)s0.Value & mask : ((int)s1.Value >> Bits) & mask;

        Sample blackSample = topIsHorizontal ? s1 : s0;

        // get the y values from the black one:
        int y1 = (int)(blackSample.Value & mask);
        int y2 = (int)((blackSample.Value >> Bits) & mask);

        int y = topIsHorizontal ? Math.Min(y1, y2) : Math.Max(y1, y2);

        bool valid = y >= s0.Y && y <= s1.Y;

        return new Sample
        {
            X = x,
            Y = y,
            Value = valid ? 1u : 0u
        };
    }

    private Sample GetEvenRowEvenColSample(int col, int row)
    {
        int cellSize = 1 << Bits;
        int halfCell = cellSize >> 1;

        // samples at 4 corners
        Sample ll = GenerateCandidate(col - 1, row - 1);
        Sample ul = GenerateCandidate(col - 1, row + 1);
        Sample lr = GenerateCandidate(col + 1, row - 1);
        Sample ur = GenerateCandidate(col + 1, row + 1);

        // samples at 4 neighbor middles
        Sample lowerMiddle = GetRowAdjustedSample(col, row - 1, ll, lr);
        Sample upperMiddle = GetRowAdjustedSample(col, row + 1, ul, ur);
        Sample middleLeft = GetColAdjustedSample(col - 1, row, ll, ul);
        Sample middleRight = GetColAdjustedSample(col + 1, row, lr, ur);

        Rect baseRect = new Rect
        {
            xmin = middleLeft.Valid ? middleLeft.X : 0,
            ymin = lowerMiddle.Valid ? lowerMiddle.Y : 0,
            xmax = middleRight.Valid ? middleRight.X : cellSize - 1,
            ymax = upperMiddle.Valid ? upperMiddle.Y : cellSize - 1,
        };

        Rect rect = OverlapCalculator.CutOut(ll, lr, ul, ur, baseRect);

        if (rect.Valid)
        {
            Sample rnd = GenerateCandidate(col, row);

            return new Sample {
                X = Lerp(rect.xmin, rect.xmax, rnd.X),
                Y = Lerp(rect.ymin, rect.ymax, rnd.Y),
                Value = 1
            };
        }

        return new Sample
        {
            X = 0,
            Y = 0,
            Value = 0
        };
    }
}
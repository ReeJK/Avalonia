﻿// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Platform;
using Avalonia.Utility;

namespace Avalonia.Media
{
    /// <summary>
    ///     Represents a sequence of glyphs from a single face of a single font at a single size, and with a single rendering style.
    /// </summary>
    public sealed class GlyphRun : IDisposable
    {
        private static readonly IComparer<ushort> s_ascendingComparer = Comparer<ushort>.Default;
        private static readonly IComparer<ushort> s_descendingComparer = new ReverseComparer<ushort>();

        private IGlyphRunImpl _glyphRunImpl;
        private GlyphTypeface _glyphTypeface;
        private double _fontRenderingEmSize;
        private Rect? _bounds;
        private int _biDiLevel;

        private ReadOnlySlice<ushort> _glyphIndices;
        private ReadOnlySlice<double> _glyphAdvances;
        private ReadOnlySlice<Vector> _glyphOffsets;
        private ReadOnlySlice<ushort> _glyphClusters;
        private ReadOnlySlice<char> _characters;

        /// <summary>
        ///     Initializes a new instance of the <see cref="GlyphRun"/> class.
        /// </summary>
        public GlyphRun()
        {

        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="GlyphRun"/> class by specifying properties of the class.
        /// </summary>
        /// <param name="glyphTypeface">The glyph typeface.</param>
        /// <param name="fontRenderingEmSize">The rendering em size.</param>
        /// <param name="glyphIndices">The glyph indices.</param>
        /// <param name="glyphAdvances">The glyph advances.</param>
        /// <param name="glyphOffsets">The glyph offsets.</param>
        /// <param name="characters">The characters.</param>
        /// <param name="glyphClusters">The glyph clusters.</param>
        /// <param name="biDiLevel">The bidi level.</param>
        /// <param name="bounds">The bound.</param>
        public GlyphRun(
            GlyphTypeface glyphTypeface,
            double fontRenderingEmSize,
            ReadOnlySlice<ushort> glyphIndices,
            ReadOnlySlice<double> glyphAdvances = default,
            ReadOnlySlice<Vector> glyphOffsets = default,
            ReadOnlySlice<char> characters = default,
            ReadOnlySlice<ushort> glyphClusters = default,
            int biDiLevel = 0,
            Rect? bounds = null)
        {
            GlyphTypeface = glyphTypeface;

            FontRenderingEmSize = fontRenderingEmSize;

            GlyphIndices = glyphIndices;

            GlyphAdvances = glyphAdvances;

            GlyphOffsets = glyphOffsets;

            Characters = characters;

            GlyphClusters = glyphClusters;

            BiDiLevel = biDiLevel;

            Initialize(bounds);
        }

        /// <summary>
        ///     Gets or sets the <see cref="Media.GlyphTypeface"/> for the <see cref="GlyphRun"/>.
        /// </summary>
        public GlyphTypeface GlyphTypeface
        {
            get => _glyphTypeface;
            set => Set(ref _glyphTypeface, value);
        }

        /// <summary>
        ///     Gets or sets the em size used for rendering the <see cref="GlyphRun"/>.
        /// </summary>
        public double FontRenderingEmSize
        {
            get => _fontRenderingEmSize;
            set => Set(ref _fontRenderingEmSize, value);
        }

        /// <summary>
        ///     Gets or sets an array of <see cref="ushort"/> values that represent the glyph indices in the rendering physical font.
        /// </summary>
        public ReadOnlySlice<ushort> GlyphIndices
        {
            get => _glyphIndices;
            set => Set(ref _glyphIndices, value);
        }

        /// <summary>
        ///     Gets or sets an array of <see cref="double"/> values that represent the advances corresponding to the glyph indices.
        /// </summary>
        public ReadOnlySlice<double> GlyphAdvances
        {
            get => _glyphAdvances;
            set => Set(ref _glyphAdvances, value);
        }

        /// <summary>
        ///     Gets or sets an array of <see cref="Vector"/> values representing the offsets of the glyphs in the <see cref="GlyphRun"/>.
        /// </summary>
        public ReadOnlySlice<Vector> GlyphOffsets
        {
            get => _glyphOffsets;
            set => Set(ref _glyphOffsets, value);
        }

        /// <summary>
        ///     Gets or sets the list of UTF16 code points that represent the Unicode content of the <see cref="GlyphRun"/>.
        /// </summary>
        public ReadOnlySlice<char> Characters
        {
            get => _characters;
            set => Set(ref _characters, value);
        }

        /// <summary>
        ///     Gets or sets a list of <see cref="int"/> values representing a mapping from character index to glyph index.
        /// </summary>
        public ReadOnlySlice<ushort> GlyphClusters
        {
            get => _glyphClusters;
            set => Set(ref _glyphClusters, value);
        }

        /// <summary>
        ///     Gets or sets the bidirectional nesting level of the <see cref="GlyphRun"/>.
        /// </summary>
        public int BiDiLevel
        {
            get => _biDiLevel;
            set => Set(ref _biDiLevel, value);
        }

        /// <summary>
        /// Gets the scale of the current <see cref="Media.GlyphTypeface"/>
        /// </summary>
        internal double Scale => FontRenderingEmSize / GlyphTypeface.DesignEmHeight;

        /// <summary>
        /// Returns <c>true</c> if the text direction is left-to-right. Otherwise, returns <c>false</c>.
        /// </summary>
        public bool IsLeftToRight => ((BiDiLevel & 1) == 0);

        /// <summary>
        ///     Gets or sets the conservative bounding box of the <see cref="GlyphRun"/>.
        /// </summary>
        public Rect Bounds
        {
            get
            {
                if (_bounds == null)
                {
                    _bounds = CalculateBounds();
                }

                return _bounds.Value;
            }
        }

        /// <summary>
        /// The platform implementation of the <see cref="GlyphRun"/>.
        /// </summary>
        public IGlyphRunImpl GlyphRunImpl
        {
            get
            {
                if (_glyphRunImpl == null)
                {
                    Initialize(null);
                }

                return _glyphRunImpl;
            }
        }

        /// <summary>
        /// Retrieves the offset from the leading edge of the <see cref="GlyphRun"/>
        /// to the leading or trailing edge of a caret stop containing the specified character hit.
        /// </summary>
        /// <param name="characterHit">The <see cref="CharacterHit"/> to use for computing the offset.</param>
        /// <returns>
        /// A <see cref="double"/> that represents the offset from the leading edge of the <see cref="GlyphRun"/>
        /// to the leading or trailing edge of a caret stop containing the character hit.
        /// </returns>
        public double GetDistanceFromCharacterHit(CharacterHit characterHit)
        {
            var distance = 0.0;

            if (characterHit.FirstCharacterIndex + characterHit.TrailingLength > Characters.End)
            {
                return Bounds.Width;
            }

            var glyphIndex = FindGlyphIndex(characterHit.FirstCharacterIndex);

            var currentCluster = _glyphClusters[glyphIndex];

            if (characterHit.TrailingLength > 0)
            {
                while (glyphIndex < _glyphClusters.Length && _glyphClusters[glyphIndex] == currentCluster)
                {
                    glyphIndex++;
                }
            }

            for (var i = 0; i < glyphIndex; i++)
            {
                if (GlyphAdvances.IsEmpty)
                {
                    var glyph = GlyphIndices[i];

                    distance += GlyphTypeface.GetGlyphAdvance(glyph) * Scale;
                }
                else
                {
                    distance += GlyphAdvances[i];
                }
            }

            return distance;
        }

        /// <summary>
        /// Retrieves the <see cref="CharacterHit"/> value that represents the character hit of the caret of the <see cref="GlyphRun"/>.
        /// </summary>
        /// <param name="distance">Offset to use for computing the caret character hit.</param>
        /// <param name="isInside">Determines whether the character hit is inside the <see cref="GlyphRun"/>.</param>
        /// <returns>
        /// A <see cref="CharacterHit"/> value that represents the character hit that is closest to the distance value.
        /// The out parameter <c>isInside</c> returns <c>true</c> if the character hit is inside the <see cref="GlyphRun"/>; otherwise, <c>false</c>.
        /// </returns>
        public CharacterHit GetCharacterHitFromDistance(double distance, out bool isInside)
        {
            // Before
            if (distance < 0)
            {
                isInside = false;

                var firstCharacterHit = FindNearestCharacterHit(_glyphClusters[0], out _);

                return IsLeftToRight ? new CharacterHit(firstCharacterHit.FirstCharacterIndex) : firstCharacterHit;
            }

            //After
            if (distance > Bounds.Size.Width)
            {
                isInside = false;

                var lastCharacterHit = FindNearestCharacterHit(_glyphClusters[_glyphClusters.Length - 1], out _);

                return IsLeftToRight ? lastCharacterHit : new CharacterHit(lastCharacterHit.FirstCharacterIndex);
            }

            //Within
            var currentX = 0.0;
            var index = 0;

            for (; index < GlyphIndices.Length; index++)
            {
                double advance;

                if (GlyphAdvances.IsEmpty)
                {
                    var glyph = GlyphIndices[index];

                    advance = GlyphTypeface.GetGlyphAdvance(glyph) * Scale;
                }
                else
                {
                    advance = GlyphAdvances[index];
                }

                if (currentX + advance >= distance)
                {
                    break;
                }

                currentX += advance;
            }

            var characterHit = FindNearestCharacterHit(GlyphClusters[index], out var width);

            var offset = GetDistanceFromCharacterHit(new CharacterHit(characterHit.FirstCharacterIndex));

            isInside = true;

            var isTrailing = distance > offset + width / 2;

            return isTrailing ? characterHit : new CharacterHit(characterHit.FirstCharacterIndex);
        }

        /// <summary>
        /// Retrieves the next valid caret character hit in the logical direction in the <see cref="GlyphRun"/>.
        /// </summary>
        /// <param name="characterHit">The <see cref="CharacterHit"/> to use for computing the next hit value.</param>
        /// <returns>
        /// A <see cref="CharacterHit"/> that represents the next valid caret character hit in the logical direction.
        /// If the return value is equal to <c>characterHit</c>, no further navigation is possible in the <see cref="GlyphRun"/>.
        /// </returns>
        public CharacterHit GetNextCaretCharacterHit(CharacterHit characterHit)
        {
            if (characterHit.TrailingLength == 0)
            {
                return FindNearestCharacterHit(characterHit.FirstCharacterIndex, out _);
            }

            var nextCharacterHit = FindNearestCharacterHit(characterHit.FirstCharacterIndex + characterHit.TrailingLength, out _);

            return new CharacterHit(nextCharacterHit.FirstCharacterIndex);
        }

        /// <summary>
        /// Retrieves the previous valid caret character hit in the logical direction in the <see cref="GlyphRun"/>.
        /// </summary>
        /// <param name="characterHit">The <see cref="CharacterHit"/> to use for computing the previous hit value.</param>
        /// <returns>
        /// A cref="CharacterHit"/> that represents the previous valid caret character hit in the logical direction.
        /// If the return value is equal to <c>characterHit</c>, no further navigation is possible in the <see cref="GlyphRun"/>.
        /// </returns>
        public CharacterHit GetPreviousCaretCharacterHit(CharacterHit characterHit)
        {
            if (characterHit.TrailingLength != 0)
            {
                return new CharacterHit(characterHit.FirstCharacterIndex);
            }

            return characterHit.FirstCharacterIndex == Characters.Start ?
                new CharacterHit(Characters.Start) :
                FindNearestCharacterHit(characterHit.FirstCharacterIndex - 1, out _);
        }

        private class ReverseComparer<T> : IComparer<T>
        {
            public int Compare(T x, T y)
            {
                return Comparer<T>.Default.Compare(y, x);
            }
        }

        /// <summary>
        /// Finds a glyph index for given character index.
        /// </summary>
        /// <param name="characterIndex">The character index.</param>
        /// <returns>
        /// The glyph index.
        /// </returns>
        public int FindGlyphIndex(int characterIndex)
        {
            if (IsLeftToRight)
            {
                if (characterIndex < _glyphClusters[0])
                {
                    return 0;
                }

                if (characterIndex > _glyphClusters[_glyphClusters.Length - 1])
                {
                    return _glyphClusters.End;
                }
            }
            else
            {
                if (characterIndex < _glyphClusters[_glyphClusters.Length - 1])
                {
                    return _glyphClusters.End;
                }

                if (characterIndex > _glyphClusters[0])
                {
                    return 0;
                }
            }

            var comparer = IsLeftToRight ? s_ascendingComparer : s_descendingComparer;

            var clusters = _glyphClusters.Buffer.Span;

            // Find the start of the cluster at the character index.
            var start = clusters.BinarySearch((ushort)characterIndex, comparer);

            // No cluster found.
            if (start < 0)
            {
                while (characterIndex > 0 && start < 0)
                {
                    characterIndex--;

                    start = clusters.BinarySearch((ushort)characterIndex, comparer);
                }

                if (start < 0)
                {
                    return -1;
                }
            }

            while (start > 0 && clusters[start - 1] == clusters[start])
            {
                start--;
            }

            return start;
        }

        /// <summary>
        /// Finds the nearest <see cref="CharacterHit"/> at given index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="width">The width of found cluster.</param>
        /// <returns>
        /// The nearest <see cref="CharacterHit"/>.
        /// </returns>
        public CharacterHit FindNearestCharacterHit(int index, out double width)
        {
            width = 0.0;

            var start = FindGlyphIndex(index);

            var currentCluster = _glyphClusters[start];

            var trailingLength = 0;

            while (start < _glyphClusters.Length && _glyphClusters[start] == currentCluster)
            {
                if (GlyphAdvances.IsEmpty)
                {
                    var glyph = GlyphIndices[start];

                    width += GlyphTypeface.GetGlyphAdvance(glyph) * Scale;
                }
                else
                {
                    width += GlyphAdvances[start];
                }

                trailingLength++;
                start++;
            }

            if (start == _glyphClusters.Length &&
                currentCluster + trailingLength != Characters.Start + Characters.Length)
            {
                trailingLength = Characters.Start + Characters.Length - currentCluster;
            }

            return new CharacterHit(currentCluster, trailingLength);
        }

        /// <summary>
        /// Calculates the bounds of the <see cref="GlyphRun"/>.
        /// </summary>
        /// <returns>
        /// The calculated bounds.
        /// </returns>
        private Rect CalculateBounds()
        {
            var scale = FontRenderingEmSize / GlyphTypeface.DesignEmHeight;

            var height = (GlyphTypeface.Descent - GlyphTypeface.Ascent + GlyphTypeface.LineGap) * scale;

            var width = 0.0;

            if (GlyphAdvances.IsEmpty)
            {
                foreach (var glyph in GlyphIndices)
                {
                    width += GlyphTypeface.GetGlyphAdvance(glyph) * Scale;
                }
            }
            else
            {
                foreach (var advance in GlyphAdvances)
                {
                    width += advance;
                }
            }

            return new Rect(0, 0, width, height);
        }

        private void Set<T>(ref T field, T value)
        {
            if (_glyphRunImpl != null)
            {
                throw new InvalidOperationException("GlyphRun can't be changed after it has been initialized.'");
            }

            field = value;
        }

        /// <summary>
        /// Initializes the <see cref="GlyphRun"/>.
        /// </summary>
        /// <param name="bounds">Optional pre computed bounds.</param>
        private void Initialize(Rect? bounds)
        {
            if (GlyphIndices.Length == 0)
            {
                throw new InvalidOperationException();
            }

            var glyphCount = GlyphIndices.Length;

            if (GlyphAdvances.Length > 0 && GlyphAdvances.Length != glyphCount)
            {
                throw new InvalidOperationException();
            }

            if (GlyphOffsets.Length > 0 && GlyphOffsets.Length != glyphCount)
            {
                throw new InvalidOperationException();
            }

            var platformRenderInterface = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();

            _glyphRunImpl = platformRenderInterface.CreateGlyphRun(this, out var width);

            if (bounds.HasValue)
            {
                _bounds = bounds;
            }
            else
            {
                var height = (GlyphTypeface.Descent - GlyphTypeface.Ascent + GlyphTypeface.LineGap) * Scale;

                _bounds = new Rect(0, 0, width, height);
            }
        }

        void IDisposable.Dispose()
        {
            _glyphRunImpl?.Dispose();
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

public class DiceRollHistory
{
    public DiceRollHistory(bool isPlayer)
    {
        IsPlayer = isPlayer;
    }


    private float _positiveAdjustmentValue = 1.05f;
    private float _negativeAdjustmentValue = 0.8f;

    private float _positiveMultiplierMax = 2.0f;
    private float _negativeMultiplierMax = 0.25f;

    private int _inARowLimit = 2;

    private List<int> _previousRolls = new();
                // num of Faces - weights per face  (ex: 6, [1, 1, 1, 1, 1, 1])
    private Dictionary<int, float[]> _diceWeights = new();
    public bool IsPlayer { get; private set; } = false;


    public float[] GetWeights(int sides)
    {
        return (float[])GetWeightsRef(sides).Clone();
    }

    private float[] GetWeightsRef(int sides)
    {
        if (_diceWeights.ContainsKey(sides))
            return _diceWeights[sides];

        _diceWeights[sides] = new float[sides];
        System.Array.Fill(_diceWeights[sides], 1);

        // Enemy NERF
        if (!IsPlayer)
        {
            for (int i = 0; i < _diceWeights[sides].Length / 2; i++)
                _diceWeights[sides][_diceWeights[sides].Length - 1 - i] *= _negativeAdjustmentValue;
        }

        return _diceWeights[sides];
    }

    public void Adjust(int sides, int rolled)
    {
        // Enemy NERF
        if (!IsPlayer) return;


        float[] weights = GetWeightsRef(sides);

        for (int i = 0; i < weights.Length; i++)
        {
            if (i == rolled - 1)
                weights[i] *= _negativeAdjustmentValue;
            else
                weights[i] *= _positiveAdjustmentValue;

            weights[i] = Mathf.Clamp(weights[i], _negativeMultiplierMax, _positiveMultiplierMax);
        }
    }

    public int CheckRollAgainstPrevious(int numberOfSides, int rolled)
    {
        if (_previousRolls.Count < _inARowLimit)
        {
            _previousRolls.Add(rolled);
            return rolled;
        }

        if (_previousRolls.Count > _inARowLimit)
            _previousRolls.RemoveAt(0);


        if (_previousRolls.TrueForAll(x => x % 2 == 0))
        {
            if (rolled % 2 == 0)
                rolled = rolled + 1 < numberOfSides ? rolled + 1 : rolled - 1;
        }
        else if (_previousRolls.TrueForAll(x => x % 2 == 1))
        {
            if (rolled % 2 == 1)
                rolled = rolled + 1 < numberOfSides ? rolled + 1 : rolled - 1; /// weirdly did negative at some point? with 1 as value though so it returned 0
        }

        rolled = Mathf.Clamp(rolled, 1, numberOfSides);

        _previousRolls.Add(rolled);
        return rolled;
    }
}

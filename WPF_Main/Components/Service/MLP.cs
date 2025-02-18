﻿using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;

[Serializable]
public class NeuralNetwork
{
    //Фундаментальные параметры 
    private int[] layers;//Слои
    private float[][] neurons;//Нейроны
    private float[][] biases;//Смещения
    private float[][][] weights;//Веса
    private int[] activations;//Активационные слои

    public float fitness = 0;//Параметр пригодности сети

    public float learningRate = 0.01f;//скорость обучения
    public float cost = 0;


    //хз зачем нужны, так как нигде не используются
    private float[][] deltaBiases;//Дельта смещения 
    private float[][][] deltaWeights;//Веса
    private int deltaCount;

    private Random rand;

    public NeuralNetwork(int[] layers, string[] layerActivations)
    {
        rand = new Random();
        this.layers = new int[layers.Length];
        for (int i = 0; i < layers.Length; i++)
        {
            this.layers[i] = layers[i];
        }
        activations = new int[layers.Length - 1];
        for (int i = 0; i < layers.Length - 1; i++)
        {
            string action = layerActivations[i];
            switch (action)
            {
                case "sigmoid":
                    activations[i] = 0;
                    break;
                case "tanh":
                    activations[i] = 1;
                    break;
                case "relu":
                    activations[i] = 2;
                    break;
                case "leakyrelu":
                    activations[i] = 3;
                    break;
                default:
                    activations[i] = 2;
                    break;
            }
        }
        InitNeurons();
        InitBiases();
        InitWeights();
    }

    private void InitNeurons()//Создание пустого массива нейронов для нейросети 
    {
        List<float[]> neuronsList = new List<float[]>();
        for (int i = 0; i < layers.Length; i++)
        {
            neuronsList.Add(new float[layers[i]]);
        }
        neurons = neuronsList.ToArray();
    }

    private void InitBiases()//Инициализация массива случайных смещений
    {
        List<float[]> biasList = new List<float[]>();
        for (int i = 1; i < layers.Length; i++)
        {
            float[] bias = new float[layers[i]];
            for (int j = 0; j < layers[i]; j++)
            {
                bias[j] = (float)(rand.NextDouble() - 0.5f);
            }
            biasList.Add(bias);
        }
        biases = biasList.ToArray();
    }

    private void InitWeights()//Инициализация массива случайных весов.
    {
        List<float[][]> weightsList = new List<float[][]>();
        for (int i = 1; i < layers.Length; i++)
        {
            List<float[]> layerWeightsList = new List<float[]>();
            int neuronsInPreviousLayer = layers[i - 1];
            for (int j = 0; j < layers[i]; j++)
            {
                float[] neuronWeights = new float[neuronsInPreviousLayer];
                for (int k = 0; k < neuronsInPreviousLayer; k++)
                {
                    neuronWeights[k] = (float)(rand.NextDouble() - 0.5f);
                }
                layerWeightsList.Add(neuronWeights);
            }
            weightsList.Add(layerWeightsList.ToArray());
        }
        weights = weightsList.ToArray();
    }

    public float[] FeedForward(float[] inputs)//Прямое распространение от входных данных до выходных
    {
        for (int i = 0; i < inputs.Length; i++)
        {
            neurons[0][i] = inputs[i];
        }
        for (int i = 1; i < layers.Length; i++)
        {
            int layer = i - 1;
            for (int j = 0; j < layers[i]; j++)
            {
                float value = 0f;
                for (int k = 0; k < layers[i - 1]; k++)
                {
                    value += weights[i - 1][j][k] * neurons[i - 1][k];
                }
                neurons[i][j] = activate(value + biases[i - 1][j], layer);
            }
        }
        return neurons[layers.Length - 1];
    }
    //Реализация обратного распространения ошибки.
    public float activate(float value, int layer)//Все функции активации
    {
        switch (activations[layer])
        {
            case 0:
                return sigmoid(value);
            case 1:
                return tanh(value);
            case 2:
                return relu(value);
            case 3:
                return leakyrelu(value);
            default:
                return sigmoid(value);
        }
    }
    public float activateDer(float value, int layer)//Производные функций активации
    {
        switch (activations[layer])
        {
            case 0:
                return sigmoidDer(value);
            case 1:
                return tanhDer(value);
            case 2:
                return reluDer(value);
            case 3:
                return leakyreluDer(value);
            default:
                return sigmoidDer(value);
        }
    }

    //Активационные функции и их производные
    public float sigmoid(float x)//Сигмоида
    {
        float k = (float)Math.Exp(x);
        return k / (1.0f + k);
    }
    public float tanh(float x)//Тангенс
    {
        return (float)Math.Tanh(x);
    }
    public float relu(float x)//ReLu - ноль при отрицательных х, f(x) = x при положительных
    {
        return (0 >= x) ? 0 : x;
    }
    public float leakyrelu(float x)//Leaky ReLu - f(x)=0,01x при отрицательных х, f(x) = x при положительных
    {
        return (0 >= x) ? 0.01f * x : x;
    }
    public float sigmoidDer(float x)//Производная сигмоиды
    {
        return x * (1 - x);
    }
    public float tanhDer(float x)//Производная тангенса
    {
        return 1 - (x * x);
    }
    public float reluDer(float x)//Производная ReLu
    {
        return (0 >= x) ? 0 : 1;
    }
    public float leakyreluDer(float x)//Производная Leaky ReLu
    {
        return (0 >= x) ? 0.01f : 1;
    }

    public void BackPropagate(float[] inputs, float[] expected)//Обратное распространение
    {
        float[] output = FeedForward(inputs);//runs feed forward to ensure neurons are populated correctly

        cost = 0;
        for (int i = 0; i < output.Length; i++) cost += (float)Math.Pow(output[i] - expected[i], 2);//calculated cost of network
        cost = cost / 2;//this value is not used in calculions, rather used to identify the performance of the network

        float[][] gamma;


        List<float[]> gammaList = new List<float[]>();
        for (int i = 0; i < layers.Length; i++)
        {
            gammaList.Add(new float[layers[i]]);
        }
        gamma = gammaList.ToArray();//gamma initialization

        int layer = layers.Length - 2;
        for (int i = 0; i < output.Length; i++) gamma[layers.Length - 1][i] = (output[i] - expected[i]) * activateDer(output[i], layer);//Gamma calculation
        for (int i = 0; i < layers[layers.Length - 1]; i++)//calculates the w' and b' for the last layer in the network
        {
            biases[layers.Length - 2][i] -= gamma[layers.Length - 1][i] * learningRate;
            for (int j = 0; j < layers[layers.Length - 2]; j++)
            {

                weights[layers.Length - 2][i][j] -= gamma[layers.Length - 1][i] * neurons[layers.Length - 2][j] * learningRate;//*learning 
            }
        }

        for (int i = layers.Length - 2; i > 0; i--)//runs on all hidden layers
        {
            layer = i - 1;
            for (int j = 0; j < layers[i]; j++)//outputs
            {
                gamma[i][j] = 0;
                for (int k = 0; k < gamma[i + 1].Length; k++)
                {
                    gamma[i][j] = gamma[i + 1][k] * weights[i][k][j];
                }
                gamma[i][j] *= activateDer(neurons[i][j], layer);//calculate gamma
            }
            for (int j = 0; j < layers[i]; j++)//itterate over outputs of layer
            {
                biases[i - 1][j] -= gamma[i][j] * learningRate;//modify biases of network
                for (int k = 0; k < layers[i - 1]; k++)//itterate over inputs to layer
                {
                    weights[i - 1][j][k] -= gamma[i][j] * neurons[i - 1][k] * learningRate;//modify weights of network
                }
            }
        }
    }

    //Реализация генетического алгоритма.

    public void Mutate(int high, float val)//Использование простой мутации для реализации генетического алгоритма(процесса)
    {
        for (int i = 0; i < biases.Length; i++)
        {
            for (int j = 0; j < biases[i].Length; j++)
            {
                biases[i][j] = (rand.NextDouble() * high <= 2) ? biases[i][j] += (float)(rand.NextDouble()*2*val - val) : biases[i][j];
            }
        }

        for (int i = 0; i < weights.Length; i++)
        {
            for (int j = 0; j < weights[i].Length; j++)
            {
                for (int k = 0; k < weights[i][j].Length; k++)
                {
                    weights[i][j][k] = (rand.NextDouble() * high <= 2) ? weights[i][j][k] += (float)(rand.NextDouble() * 2 * val - val) : weights[i][j][k];
                }
            }
        }
    }

    public int CompareTo(NeuralNetwork other) //Проверка работы генетической реализации для пригодности сети
    {
        if (other == null) return 1;

        if (fitness > other.fitness)
            return 1;
        else if (fitness < other.fitness)
            return -1;
        else
            return 0;
    }

    public NeuralNetwork copy(NeuralNetwork nn) //Создание глубокой копии, чтобы гарантировать сериализацию массивов.
    {
        for (int i = 0; i < biases.Length; i++)
        {
            for (int j = 0; j < biases[i].Length; j++)
            {
                nn.biases[i][j] = biases[i][j];
            }
        }
        for (int i = 0; i < weights.Length; i++)
        {
            for (int j = 0; j < weights[i].Length; j++)
            {
                for (int k = 0; k < weights[i][j].Length; k++)
                {
                    nn.weights[i][j][k] = weights[i][j][k];
                }
            }
        }
        return nn;
    }

    //Функции сохранения и загрузки данных
    public void Load(string path)//Загрузка смещений и весов в нейронную сеть из файла.
    {
        TextReader tr = new StreamReader(path);
        int NumberOfLines = (int)new FileInfo(path).Length;
        string[] ListLines = new string[NumberOfLines];
        int index = 1;
        for (int i = 1; i < NumberOfLines; i++)
        {
            ListLines[i] = tr.ReadLine();
        }
        tr.Close();
        if (new FileInfo(path).Length > 0)
        {
            for (int i = 0; i < biases.Length; i++)
            {
                for (int j = 0; j < biases[i].Length; j++)
                {
                    biases[i][j] = float.Parse(ListLines[index]);
                    index++;
                }
            }

            for (int i = 0; i < weights.Length; i++)
            {
                for (int j = 0; j < weights[i].Length; j++)
                {
                    for (int k = 0; k < weights[i][j].Length; k++)
                    {
                        weights[i][j][k] = float.Parse(ListLines[index]); ;
                        index++;
                    }
                }
            }
        }
    }
    public void Save(string path)//Сохранение весов и смещений в файл.
    {
        File.Create(path).Close();
        StreamWriter writer = new StreamWriter(path, true);

        for (int i = 0; i < biases.Length; i++)
        {
            for (int j = 0; j < biases[i].Length; j++)
            {
                writer.WriteLine(biases[i][j]);
            }
        }

        for (int i = 0; i < weights.Length; i++)
        {
            for (int j = 0; j < weights[i].Length; j++)
            {
                for (int k = 0; k < weights[i][j].Length; k++)
                {
                    writer.WriteLine(weights[i][j][k]);
                }
            }
        }
        writer.Close();
    }    
}
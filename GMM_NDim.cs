using Mapack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMM
{
    class GMM_NDim
    {
        public Matrix inputMatrix { get; set; }
        int k;  // number of clusters or mixture components
        int dim; // number of dimensions for data
        double[] phi = null;
        public Matrix[] mu = null;
        public Matrix[] sigma = null;
        double[,] pdf = null;
        public double[,] Gamma = null;
        public GMM_NDim(int k, int dim, Matrix inputMatrix)
        {
            this.k = k;
            this.dim = dim;
            this.inputMatrix = inputMatrix;
            mu = new Matrix[k];  // mean for cluster k
            for (int i = 0; i < k; i++)
                mu[i] = new Matrix(1, dim);

            sigma = new Matrix[k];   // cov matrix for cluster k
            for (int i = 0; i < k; i++)
                sigma[i] = new Matrix(dim, dim);

            pdf = new double[inputMatrix.Rows, k];
            // calculated pdf for each data point based on mean and var for cluster k

            Gamma = new double[inputMatrix.Rows, k];
            // probablity matrix for each data point belonging to cluster k
            // i.e., probablity that a data point belongs to cluster i

            phi = new double[k];    // prior probabilities for each cluster
        }

        public void ComputeGMM_ND()
        {
            //System.Diagnostics.Debug.WriteLine("reach 6");
            Random rand = new Random();
            // ----------Initialization step - randomly select k data poits to act as means
            List<int> RList = new List<int>();
            for (int i = 0; i < k; i++)
            {
                int rpos = rand.Next(inputMatrix.Rows);
                if (RList.Contains(rpos))
                    rpos = rand.Next(inputMatrix.Rows);
                for (int m = 0; m < dim; m++)
                    mu[i][0, m] = inputMatrix[rpos, m];
            }
            //System.Diagnostics.Debug.WriteLine("reach 7");

            // set the variance of each cluster to be the overall variance
            Matrix varianceOfData = ComputeCoVariance(inputMatrix);
            for (int i = 0; i < varianceOfData.Rows; i++)
            {
                for (int j = 0; j < varianceOfData.Columns; j++)
                    varianceOfData[i, j] = varianceOfData[i, j];// Math.Sqrt(varianceOfData[i, j]);
            }
            for (int i = 0; i < k; i++)
            {
                sigma[i] = varianceOfData.Clone();
            }

            // set prior probablities of each cluster to be uniform
            for (int i = 0; i < k; i++)
            {
                phi[i] = 1.0 / k;
            }


            //--------------------------end initialization-------------------------------------
            //System.Diagnostics.Debug.WriteLine("reach 8");

            // ---------------------------Expectation Maximization------------------------------
            #region ExpecMaxi
            for (int n = 0; n < 1000; n++)
            {
                //---------perform Expectation step---------------------
                for (int i = 0; i < inputMatrix.Rows; i++)
                {
                    for (int k1 = 0; k1 < k; k1++)
                    {
                        pdf[i, k1] = GaussianMV(inputMatrix, i, dim, mu[k1], sigma[k1]);
                    }
                }
                double[] Gdenom = new double[inputMatrix.Rows];
                for (int i = 0; i < inputMatrix.Rows; i++) // denominator for Gamma
                {
                    double sum = 0;
                    for (int k1 = 0; k1 < k; k1++)
                    {
                        sum = sum + phi[k1] * pdf[i, k1];
                    }
                    Gdenom[i] = sum;
                }

                for (int i = 0; i < inputMatrix.Rows; i++)
                {
                    for (int k1 = 0; k1 < k; k1++)
                    {
                        Gamma[i, k1] = (phi[k1] * pdf[i, k1]) / Gdenom[i];
                    }
                }

                //-------------------end Expectation--------------------
                // System.Diagnostics.Debug.WriteLine("reach 9");

                //---------perform Maximization Step--------------------
                //----------update phi--------------
                for (int k1 = 0; k1 < k; k1++)
                {
                    double sum = 0;
                    for (int i = 0; i < inputMatrix.Rows; i++)
                    {
                        sum += Gamma[i, k1];
                    }
                    phi[k1] = sum / (inputMatrix.Rows);
                }
                //---------------------------------
                //System.Diagnostics.Debug.WriteLine("reach 10");

                //-------------update mu-----------
                double[,] MuNumer = new double[k, dim];
                for (int k1 = 0; k1 < k; k1++)
                {
                    double[] sum = new double[dim];
                    for (int i = 0; i < inputMatrix.Rows; i++)
                    {
                        for (int m = 0; m < dim; m++)
                            sum[m] += Gamma[i, k1] * inputMatrix[i, m];
                    }
                    for (int m = 0; m < dim; m++)
                        MuNumer[k1, m] = sum[m];
                }

                double[] MuDenom = new double[k];
                for (int k1 = 0; k1 < k; k1++)
                {
                    double sum = 0;
                    for (int i = 0; i < inputMatrix.Rows; i++)
                    {
                        sum += Gamma[i, k1];
                    }
                    MuDenom[k1] = sum;
                }
                for (int i = 0; i < k; i++)
                {
                    for (int m = 0; m < dim; m++)
                        mu[i][0, m] = MuNumer[i, m] / MuDenom[i];
                }
                //-----------------------------------
                // System.Diagnostics.Debug.WriteLine("reach 11");

                //-------------update sigma----------
                Matrix[] VarianceNumer = new Matrix[k];
                for (int k1 = 0; k1 < k; k1++)
                {
                    Matrix sum = new Matrix(dim, dim);

                    for (int i = 0; i < inputMatrix.Rows; i++)
                    {
                        Matrix xi = new Matrix(1, dim);
                        for (int m = 0; m < dim; m++)
                            xi[0, m] = inputMatrix[i, m];
                        sum += ((xi - mu[k1]).Transpose() * (xi - mu[k1])) * Gamma[i, k1];
                    }
                    VarianceNumer[k1] = sum;
                }

                //System.Diagnostics.Debug.WriteLine("reach 12");

                for (int i = 0; i < k; i++)
                    sigma[i] = VarianceNumer[i] * (1 / MuDenom[i]);
                //--------------end update Sigma--------
                //System.Diagnostics.Debug.WriteLine("reach 13");

                //---------------end Maximization-------------------------------
            } 
            #endregion
            var G = Gamma;
            //System.Diagnostics.Debug.WriteLine("Out");

        }

        public void ComputeGMM_NDTesting()
        {
            //System.Diagnostics.Debug.WriteLine("reach 6");
            Random rand = new Random();
            // ----------Initialization step - randomly select k data poits to act as means
            List<int> RList = new List<int>();
            for (int i = 0; i < k; i++)
            {
                int rpos = rand.Next(inputMatrix.Rows);
                if (RList.Contains(rpos))
                    rpos = rand.Next(inputMatrix.Rows);
                for (int m = 0; m < dim; m++)
                    mu[i][0, m] = inputMatrix[rpos,m];
            }
            //System.Diagnostics.Debug.WriteLine("reach 7");

            // set the variance of each cluster to be the overall variance
            Matrix varianceOfData = ComputeCoVariance(inputMatrix);
            for (int i = 0; i < varianceOfData.Rows; i++)
            {
                for (int j = 0; j < varianceOfData.Columns; j++)
                    varianceOfData[i, j] = varianceOfData[i, j] ;// Math.Sqrt(varianceOfData[i, j]);
            }
            for (int i = 0; i < k; i++)
            {
                sigma[i] = varianceOfData.Clone();  
            }

            // set prior probablities of each cluster to be uniform
            for (int i = 0; i < k; i++)
            {
                phi[i] = 1.0 / k;
            }
            //--------------------------end initialization-------------------------------------
            //System.Diagnostics.Debug.WriteLine("reach 8");

            // ---------------------------Expectation Maximization------------------------------
            for (int n = 0; n < 1000; n++)
            {
                //---------perform Expectation step---------------------
                for (int i = 0; i < inputMatrix.Rows; i++)
                {
                    for (int k1 = 0; k1 < k; k1++)
                    {
                        pdf[i, k1] = GaussianMV(inputMatrix, i, dim, mu[k1], sigma[k1]);
                    }
                }
                double[] Gdenom = new double[inputMatrix.Rows];
                for (int i = 0; i < inputMatrix.Rows; i++) // denominator for Gamma
                {
                    double sum = 0;
                    for (int k1 = 0; k1 < k; k1++)
                    {
                        sum = sum + phi[k1] * pdf[i, k1];
                    }
                    Gdenom[i] = sum;
                }

                for (int i = 0; i < inputMatrix.Rows; i++)
                {
                    for (int k1 = 0; k1 < k; k1++)
                    {
                        Gamma[i, k1] = (phi[k1] * pdf[i, k1]) / Gdenom[i];
                    }
                }

                //-------------------end Expectation--------------------
               // System.Diagnostics.Debug.WriteLine("reach 9");

                //---------perform Maximization Step--------------------
                //----------update phi--------------
                for (int k1 = 0; k1 < k; k1++)
                {
                    double sum = 0;
                    for (int i = 0; i < inputMatrix.Rows; i++)
                    {
                        sum += Gamma[i, k1];
                    }
                    phi[k1] = sum / (inputMatrix.Rows);
                }
                //---------------------------------
                //System.Diagnostics.Debug.WriteLine("reach 10");

                //-------------update mu-----------
                double[,] MuNumer = new double[k, dim];
                for (int k1 = 0; k1 < k; k1++)
                {
                    double[] sum = new double[dim];
                    for (int i = 0; i < inputMatrix.Rows; i++)
                    {
                        for (int m =0; m < dim; m++)
                            sum[m] += Gamma[i, k1] * inputMatrix[i, m];
                    }
                    for (int m = 0; m < dim; m++)
                        MuNumer[k1, m] = sum[m];
                }

                double[] MuDenom = new double[k];
                for (int k1 = 0; k1 < k; k1++)
                {
                    double sum = 0;
                    for (int i = 0; i < inputMatrix.Rows; i++)
                    {
                        sum += Gamma[i, k1];
                    }
                    MuDenom[k1] = sum;
                }
                for (int i = 0; i < k; i++)
                {
                    for (int m = 0; m < dim; m++)
                        mu[i][0, m] = MuNumer[i, m] / MuDenom[i];
                }
                //-----------------------------------
               // System.Diagnostics.Debug.WriteLine("reach 11");

                //-------------update sigma----------
                Matrix[] VarianceNumer = new Matrix[k];
                for (int k1 = 0; k1 < k; k1++)
                {
                    Matrix sum = new Matrix(dim, dim);

                    for (int i = 0; i < inputMatrix.Rows; i++)
                    {
                        Matrix xi = new Matrix(1, dim);
                        for (int m = 0; m < dim; m++)
                            xi[0, m] = inputMatrix[i, m]; 
                        sum += ((xi - mu[k1]).Transpose() * (xi - mu[k1])) * Gamma[i, k1];
                    }
                    VarianceNumer[k1] = sum;
                }

                //System.Diagnostics.Debug.WriteLine("reach 12");

                for (int i = 0; i < k; i++)
                    sigma[i] = VarianceNumer[i] * (1 / MuDenom[i]);
                //--------------end update Sigma--------
                //System.Diagnostics.Debug.WriteLine("reach 13");

                //---------------end Maximization-------------------------------
            }
            var G = Gamma;
            //System.Diagnostics.Debug.WriteLine("Out");

        }

        public void ComputeGMM_ND2() {
            SwarmSystem1 ss = new SwarmSystem1(50, inputMatrix, k, dim);
            ss.Initialize();
            SwarmResult1 sr = ss.DoPSO();



            //===========Parse===========
            double[] x = sr.X;

            Debug.WriteLine("[{0}]", string.Join(", ", x));

            #region Parsing_array_to_Parameters
            int index = 0;
            //parsing phi
            for (int i = 0; i < k; i++)
            {
                phi[i] = x[index];
                index++;
                // Debug.WriteLine("[{0}]", string.Join(", ", phi));

            }
            //parsing mu
            for (int i = 0; i < k; i++)
            {
                for (int j = 0; j < dim; j++)
                {
                    mu[i][0, j] = x[index];
                    index++;
                }
            }
            //parsing sigma
            for (int i = 0; i < k; i++)
            {
                for (int j = 0; j < dim; j++)
                {
                    for (int h = 0; h < dim; h++)
                    {
                        sigma[i][j, h] = x[index];
                        index++;
                    }
                }
            }
            #endregion
            //===========================
            ////compute gamma
            for (int i = 0; i < inputMatrix.Rows; i++)
            {
                for (int k1 = 0; k1 < k; k1++)
                {
                    pdf[i, k1] = GaussianMV(inputMatrix, i, dim, mu[k1], sigma[k1]);
                }
            }
            double[] Gdenom = new double[inputMatrix.Rows];
            for (int i = 0; i < inputMatrix.Rows; i++) // denominator for Gamma
            {
                double sum = 0;
                for (int k1 = 0; k1 < k; k1++)
                {
                    sum = sum + phi[k1] * pdf[i, k1];
                }
                Gdenom[i] = sum;
            }

            for (int i = 0; i < inputMatrix.Rows; i++)
            {
                for (int k1 = 0; k1 < k; k1++)
                {
                    Gamma[i, k1] = (phi[k1] * pdf[i, k1]) / Gdenom[i];
                }
            }
            Debug.WriteLine(" Ending gamma");

            ///'ending compute gamma


        }

        public Matrix ComputeCoVariance(Matrix data)
        {
            Matrix data2 = data.Clone();
            double[] sum = new double[dim];
            for (int i = 0; i < data.Rows; i++)
            {
                for (int m = 0; m < dim; m++)
                    sum[m] += data[i, m];
            }
            for (int i = 0; i < data.Rows; i++)
            {
                for (int m = 0; m < dim; m++)
                    data2[i, m] -= (sum[m] / data.Rows);
            }
            Matrix dt = (data2.Transpose() * data2);
            for (int i = 0; i < dt.Rows; i++)
            {
                for (int m = 0; m < dim; m++)
                    dt[i, m] /= data.Rows - 1;
            }
            return dt;
        }

        public double GaussianMV(Matrix xdata, int index, int dim, Matrix mean, Matrix cov)  // n-D gaussian
        {
            Matrix xi = new Matrix(1, dim);
            for (int i = 0; i < dim; i++)
                xi[0, i] = xdata[index, i];  
            var exp = (xi - mean) * cov.Inverse * (xi - mean).Transpose();
            var exp2 = exp[0, 0] * -0.5;
            double res = 1 / (Math.Sqrt(cov.Determinant) * Math.Sqrt(Math.Pow(2.0 * Math.PI, dim))) *
                Math.Exp(exp2);
            return res;
        }
    }
    class SwarmSystem1
    {
        public int numberofinteration = 200;

        public Matrix inputMatrix { get; set; }
        public int dim_matrix;
        public int k;

        public Matrix[] mu = null;
        public Matrix[] sigma = null;
        public double[,] Gamma = null;
        double[] phi = null;

        public SwarmSystem1(int snum, Matrix inputMatrix, int k, int dim_x)
        {
            this.swarmNum = snum;
            this.SwarmDim = k + k * dim_x + k * dim_x * dim_x; ;
            this.G = new double[SwarmDim];
            this.P = new double[SwarmDim];
            this.inputMatrix = inputMatrix;
            this.dim_matrix = dim_x;
            this.k = k;

            //initialize
            mu = new Matrix[k];  // mean for cluster k
            for (int i = 0; i < k; i++)
                mu[i] = new Matrix(1, dim_matrix);

            sigma = new Matrix[k];   // cov matrix for cluster k
            for (int i = 0; i < k; i++)
                sigma[i] = new Matrix(dim_matrix, dim_matrix);

            Gamma = new double[inputMatrix.Rows, k];
            // probablity matrix for each data point belonging to cluster k
            // i.e., probablity that a data point belongs to cluster i
            phi = new double[k];    // prior probabilities for each cluster


        }
        int swarmNum;
        public int SwarmNum
        {
            get { return swarmNum; }
        }

        int SwarmDim;
        public int swarmDim
        {
            get { return SwarmDim; }
        }

        public List<Particle2> PList = new List<Particle2>();
        public double[] P { get; set; }
        //public double Py { get; set; }
        public double[] G { get; set; }
        //public double Gy { get; set; }
        public void Initialize()
        {

            Random ran = new Random();
            for (int J = 0; J < 50; J++) // 50 particles in swarm
            {
                Particle2 p = new Particle2(SwarmDim);
                p.W = 0.73;

                p.C1 = 1.4;
                p.C2 = 1.5;

                Random rand = new Random();
                double num = ran.NextDouble();
                ////initialize Postion
                #region InitializePOSTION
                //for (int i = 0; i < SwarmDim; i++)
                //{
                //    p.POSITION[i] = ran.NextDouble() * 20;

                //}

                //

                //for (int i = 0; i < SwarmDim; i++)
                //{
                //    if (num > 0.5)
                //    {
                //        p.POSITION[i] = -1 * p.POSITION[i];
                //    }
                //}

                ////////////////////////////////
                ///mu
                List<int> RList = new List<int>();
                for (int i = 0; i < k; i++)
                {
                    int rpos = rand.Next(inputMatrix.Rows);
                    if (RList.Contains(rpos))
                        rpos = rand.Next(inputMatrix.Rows);
                    for (int m = 0; m < dim_matrix; m++)
                        mu[i][0, m] = inputMatrix[rpos, m];
                }
                //sigma
                Matrix varianceOfData = ComputeCoVariance(inputMatrix);
                for (int i = 0; i < varianceOfData.Rows; i++)
                {
                    for (int j = 0; j < varianceOfData.Columns; j++)
                        varianceOfData[i, j] = varianceOfData[i, j];// Math.Sqrt(varianceOfData[i, j]);
                }
                for (int i = 0; i < k; i++)
                {
                    sigma[i] = varianceOfData.Clone();
                }
                //phi
                for (int i = 0; i < k; i++)
                {
                    phi[i] = 1.0 / k;
                }
                ////////////////==================
                //parsing phi
                int index = 0;

                for (int i = 0; i < k; i++)
                {
                    // Debug.WriteLine("index is " + index);

                    p.POSITION[index] = phi[i];
                    index++;
                }
                //parsing mu
                for (int i = 0; i < k; i++)
                {
                    for (int j = 0; j < dim_matrix; j++)
                    {
                        //   Debug.WriteLine("index is " + index);

                        p.POSITION[index] = mu[i][0, j];
                        index++;
                    }
                }
                //parsing sigma
                for (int i = 0; i < k; i++)
                {
                    for (int j = 0; j < dim_matrix; j++)
                    {
                        for (int h = 0; h < dim_matrix; h++)
                        {
                            p.POSITION[index] = sigma[i][j, h];
                            index++;
                        }
                    }
                }



                ////////End initialize 
                #endregion



                /////initialize velocity
                for (int i = 0; i < SwarmDim; i++)
                {
                    p.V[i] = ran.NextDouble() * 5;
                }

                num = ran.NextDouble();

                for (int i = 0; i < SwarmDim; i++)
                {
                    p.V[i] = -1 * p.V[i];
                }

                PList.Add(p);
            }
        }

        public double FunctionToSolve3(double[] x)
        {
            // Rosenbrock function
            int index = 0;
            //parsing phi
            for (int i = 0; i < k; i++)
            {
                // Debug.WriteLine("index is " + index);

                phi[i] = x[index];
                index++;
            }
            //parsing mu
            for (int i = 0; i < k; i++)
            {
                for (int j = 0; j < dim_matrix; j++)
                {
                    //   Debug.WriteLine("index is " + index);

                    mu[i][0, j] = x[index];
                    index++;
                }
            }
            //parsing sigma
            for (int i = 0; i < k; i++)
            {
                for (int j = 0; j < dim_matrix; j++)
                {
                    for (int h = 0; h < dim_matrix; h++)
                    {
                        //     Debug.WriteLine("index is " + index);

                        sigma[i][j, h] = x[index];
                        index++;
                    }
                }
            }

            //////////////real fucntion1
            double res = 0;
            //Matrix res1 = new Matrix(1, k);

            //for (int j = 0; j < k; j++)
            //{
            //    //Debug.WriteLine("sigma: \n" + sigma[j].ToString());
            //    //Debug.WriteLine("mu: \n" + mu[j].ToString());

            //    res1 = mu[j] * sigma[j];
            //    for (int l = 0; l < k; l++)
            //    {
            //        res += Math.Abs(res1[0, l]);
            //    }
            //}


            //double res_2 = 0;
            //for (int i = 0; i < k; i++)
            //{
            //    res_2 += Math.Abs(phi[i]);
            //}
            //res_2 = Math.Abs(res_2 - 1);
            //return Math.Abs(res_2)+Math.Abs(res);
            /////////////2


            for (int i = 0; i < inputMatrix.Rows; i++)
            {
                double inter_res = 0;
                for (int j = 0; j < k; j++)
                {
                    inter_res += phi[j] * GaussianMV(inputMatrix, i, dim_matrix, mu[j], sigma[j]);
                }
                res += Math.Log(inter_res);
            }

            double res_2 = 0;
            for (int i = 0; i < k; i++)
            {
                res_2 += phi[i];
            }
            res_2 = res_2 - 1;
            return Math.Abs(res + res_2);


        }



        public SwarmResult1 DoPSO() // Particle movement to achieve
        { // for particle swarm optimization
            for (int i = 0; i < SwarmDim; i++)
            {
                G[i] = PList[0].POSITION[i];
            }
            numberofinteration = 15000;

            for (int j = 0; j < 200; j++) // iterations
            {
                System.Diagnostics.Debug.WriteLine("interation:  "+j);

                // find best position in the swarm
                for (int i = 0; i < SwarmDim; i++)
                {
                    P[i] = PList[0].POSITION[i];
                }

                foreach (Particle2 pt in PList)
                {
                    if (Math.Abs(FunctionToSolve3(pt.POSITION)) < Math.Abs(FunctionToSolve3(P)))
                    {
                        for (int i = 0; i < SwarmDim; i++)
                        {
                            P[i] = pt.POSITION[i];
                        }
                    }
                }
                if (Math.Abs(FunctionToSolve3(P)) < Math.Abs(FunctionToSolve3(G)))
                {
                    for (int i = 0; i < SwarmDim; i++)
                    {
                        G[i] = P[i];
                    }
                }

                //update
                foreach (Particle2 pt in PList)
                {
                    pt.UpdateVelocity(P, G);
                    pt.UpdatePosition();
                }
            }
            SwarmResult1 sr = new SwarmResult1
            {
                SwarmId = swarmNum,
                X = G,
                FunctionValue = FunctionToSolve3(G)
            };
            return sr;
        }


        public Matrix ComputeCoVariance(Matrix data)
        {
            Matrix data2 = data.Clone();
            double[] sum = new double[dim_matrix];
            for (int i = 0; i < data.Rows; i++)
            {
                for (int m = 0; m < dim_matrix; m++)
                    sum[m] += data[i, m];
            }
            for (int i = 0; i < data.Rows; i++)
            {
                for (int m = 0; m < dim_matrix; m++)
                    data2[i, m] -= (sum[m] / data.Rows);
            }
            Matrix dt = (data2.Transpose() * data2);
            for (int i = 0; i < dt.Rows; i++)
            {
                for (int m = 0; m < dim_matrix; m++)
                    dt[i, m] /= data.Rows - 1;
            }
            return dt;
        }


        public double GaussianMV(Matrix xdata, int index, int dim, Matrix mean, Matrix cov)  // n-D gaussian
        {
            Matrix xi = new Matrix(1, dim);
            for (int i = 0; i < dim; i++)
                xi[0, i] = xdata[index, i];
            var exp = (xi - mean) * cov.Inverse * (xi - mean).Transpose();
            var exp2 = exp[0, 0] * -0.5;
            double res = 1 / (Math.Sqrt(cov.Determinant) * Math.Sqrt(Math.Pow(2.0 * Math.PI, dim))) *
                Math.Exp(exp2);
            return res;
        }

    }


    class SwarmResult1 : IComparable<SwarmResult1>
    {
        public int SwarmId { get; set; }
        public double[] X { get; set; }
        public double FunctionValue { get; set; }
        #region nouse
        public override string ToString()
        {
            return "SwarmId:" + SwarmId.ToString() +
            " X=" + X[0].ToString() +
            " Y=" + X[1].ToString() +
            "Function Value=" + FunctionValue.ToString();
        }
        #endregion
        public int CompareTo(SwarmResult1 other)
        {
            return this.FunctionValue.CompareTo(other.FunctionValue);
        }
    }

    class Particle2
    {
        public int dim { get; set; }
        public double W { get; set; } // inertia or weight
        public double C1 { get; set; } // cognitive social const
        public double C2 { get; set; }
        //double[] C { get; set; } // cognitive social const
        Random ran = new Random();
        public double[] POSITION { get; set; } ////////////////// postion vector
        public double[] V { get; set; } //////////////// velocity vector
        public Particle2(int dim)
        {
            this.dim = dim;
            this.POSITION = new double[dim];
            this.V = new double[dim];

        }

        public void UpdateVelocity(double[] P, double[] G)
        {
            // P is the current best position of any particle in the swarm
            // G is the global best position discovered so far


            for (int i = 0; i < dim; i++)
            {
                V[i] = W * V[i] + C1 * ran.NextDouble() * (P[i] - POSITION[i]) + C2 * ran.NextDouble() * (G[i] - POSITION[i]);
                if (V[i] > 5)
                    V[i] = 5;
                if (V[i] < -5)
                    V[i] = -5;
            }

        }

        public void UpdatePosition()
        {
            for (int i = 0; i < dim; i++)
            {
                POSITION[i] = POSITION[i] + V[i];
                // we need to put some bounds on the position
                if (POSITION[i] > 3000)
                    POSITION[i] = 300;
                if (POSITION[i] < -3000)
                    POSITION[i] = 0;
            }


            //Xy = Xy + Vy;
            //if (Xy > 20)
            //    Xy = 20;
            //if (Xy < -20)
            //    Xy = -20;
        }
    }
}

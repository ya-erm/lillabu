﻿using LilaApp.Models;
using LilaApp.Models.Railways;
using System;
using System.Collections.Generic;
using System.Threading;

namespace LilaApp.Algorithm
{
    public class AmplifierFinalSolver : IFinalTaskSolver
    {
        #region Implementation of IFinalTaskSolver

        /// <inheritdoc />
        public FinalAnswer Solve(Model model, IDirectTaskSolver checker)
        {
            _model = model;
            _answer = Model.Copy(model);
            _checker = checker;
            Best = new FinalAnswer(_answer, _checker.Solve(_answer));

            _chain = RailwayChain.FromModel(_answer, reduceCount: true);
            _current = _chain;

            try
            {
                Main();
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Алгоритм AmplifierFinalSolver остановлен");
            }
            finally
            {
                var ans = Model.Copy(Best.Model);
                ans.Blocks.Clear();
                ans.Blocks.AddRange(_model.Blocks);
                Best = new FinalAnswer(ans, _checker.Solve(ans));
            }

            return Best;
        }

        /// <inheritdoc />
        public CancellationToken Token { get; set; }

        /// <inheritdoc />
        public event EventHandler<FinalAnswer> OnStepEvent;

        #endregion

        private RailwayChain _chain;
        private IRailwayTemplate _current;
        private IDirectTaskSolver _checker;
        private Model _answer;
        private Model _model;

        private FinalAnswer Best { get; set; }

        private void Main()
        {
            var library = new List<string>();
            library.Add("L1L1L1");
            library.Add("L1L1");
            library.Add("L2");
            library.Add("L3");
            library.Add("L4");
            library.AddRange(RailwayTemplates.Library);
            library.Add("t2");
            library.Add("T2");
            library.Add("T2L1");
            library.Add("t2L1");
            library.Add("T2L2");
            library.Add("t2L2");
            library.Add("T2L3");
            library.Add("t2L3");

            // Перебираем каждый шаблон библиотеки
            foreach (var blueprint in library)
            {
                var answer = Model.Copy(_answer);

                if (!RailwayFactory.Default.TryBuildTemplate(out var template, out var error, blueprint, answer))
                {
                    continue;
                }

                var chain = RailwayChain.FromModel(answer);

                // Все под-шаблоны исходной цепочки, которые можно заменить на подходящий шаблон
                var subTemplates = chain.FindSubTemplates(template.Dimensions.Output);
                template.ReturnBlocksToModel(answer);

                var cur = chain[0];

                for (var j = 0; j < subTemplates.Count; j++)
                {
                    //TODO chain = chain.Copy;

                    if (!RailwayFactory.Default.TryBuildTemplate(out template, out _, blueprint, answer))
                    {
                        break;
                    }

                    var (length, start, end) = subTemplates[j];

                    var finish = true;
                    while (cur != null)
                    {
                        if (cur == start) finish = false;
                        if (cur == end) break;
                        cur = cur.Next;
                        if (cur is Railway r && r.IsHead()) break;
                    }
                    if (finish)
                        continue;

                    // Присоединяем новый шаблон
                    {
                        if (start.Prev != null) start.Prev.Next = template;
                        template.Prev = start.Prev;

                        if (end.Next != null) end.Next.Prev = template;
                        template.Next = end.Next;

                        template.Start = start.Start;
                    }

                    var result = chain.ConvertToModel(answer);
                    var price = _checker.Solve(result);

                    if (price.Result > Best.Price.Result)
                    {
                        // Проверяем на пересечения:
                        if (RailwayChain.FromModel(result).FindCrosses().Count == 0)
                        {
                            Best = new FinalAnswer(result, price);

                            OnStepEvent?.Invoke(this, Best);
                        }
                    }
                }
            }
        }
    }
}

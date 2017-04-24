﻿using gitWeb.Core.Features.Commit;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper.Execution;

namespace gitWeb.Core.GraphBuilder
{
    public class GraphBuilder
    {
        List<Commit> MainPath = new List<Commit>();
        HIndexProvider hIndexProvider = new HIndexProvider();
        private int iter = 0;

        public GraphBuilder()
        {
        }

        public void Build(Commit[] commits)
        {
            Stopwatch diagnostic = new Stopwatch();
            diagnostic.Start();
            AssignParents(commits);
            diagnostic.Stop();
            Debug.WriteLine("     AssignParents: " + diagnostic.ElapsedMilliseconds);

            diagnostic.Restart();
            CreateMainPath(commits[0]);
            diagnostic.Stop();
            Debug.WriteLine("     CreateMainPath: " + diagnostic.ElapsedMilliseconds);

            diagnostic.Restart();
            //Dictionary<string,Commit> vrlDictionaryOfCommits = new Dictionary<string, Commit[]>();
            //vrlDictionaryOfCommits = commits.ToDictionary(s => s.Sha, v => v);
            AssignParentColumn(commits[0], commits.ToList());
            diagnostic.Stop();
            Debug.WriteLine("AssignParentColumn: " + diagnostic.ElapsedMilliseconds);
            Debug.WriteLine("commits " + commits.Length);
            Debug.WriteLine("iterations " + iter);
        }

        private void AssignParents(Commit[] commits)
        {
            int index = 0;

            foreach (Commit t in commits)
            {
                index++;
                var currCommit = t;
                currCommit.SetVIndex(index);

                var currCommitParents = commits.Where(c => currCommit.Parents.Contains(c.Sha)).ToArray();
                currCommit.AssignParents(currCommitParents);
            }
        }

        private void AssignParentColumn(Commit commit, List<Commit> commits)
        {
            if (commit.handled)
            {
                return;
            }

            commit.handled = true;

            for (var i = 0; i < commit.CommitParents.Length; i++)
            {
                iter++;

                var currParent = commit.CommitParents[i];


                if (currParent.HIndex == 0)
                {
                    if (i == 0)
                    {
                        currParent.SetHIndex(commit.HIndex,commit.ColumnInternalIndex);
                        currParent.skipped = false;

                    }
                    else
                    {
                        var childrens = commits.Where(c => c.Parents.Contains(currParent.Sha) && c.Sha != commit.Sha).ToList();

                        if (childrens.Any(d => d.HIndex == 0))
                        {
                            currParent.skipped = true;
                            continue;
                        }

                        currParent.skipped = false;

                        dynamic freeIndex = hIndexProvider.GetFreeIndex(currParent.Date);
                        currParent.SetHIndex(freeIndex.Id,freeIndex.Index);
                    }
                }
                else
                {
                    if (currParent.HIndex < commit.HIndex)
                    {
                        hIndexProvider.ReleaseIndex(commit.ColumnInternalIndex, currParent.Date);
                    }
                }

                AssignParentColumn(currParent, commits);

            }

            ////for (var i = commit.CommitParents.Length; i-- > 0;)
            //for (var i = 0; i < commit.CommitParents.Length; i++)
            //{
                
            //    var commitParent = commit.CommitParents[i];

            //    if (commitParent.skipped)
            //    {
            //        continue;
            //    }

            //}
        }

        private void CreateMainPath(Commit currentCommit)
        {
            if (currentCommit == null) return;

            currentCommit.SetHIndex(1);

            MainPath.Add(currentCommit);

            if (currentCommit.CommitParents.Length > 0)
            {
                CreateMainPath(currentCommit.CommitParents[0]);
            }

        }

    }

}
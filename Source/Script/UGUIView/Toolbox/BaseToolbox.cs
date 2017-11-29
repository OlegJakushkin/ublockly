﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UBlockly.UGUI
{
    public abstract class BaseToolbox : MonoBehaviour
    {
        /// <summary>
        /// the current displayed block category
        /// </summary>
        protected string mActiveCategory;
        
        /// <summary>
        /// root objects of block views for different category
        /// </summary>
        protected Dictionary<string, GameObject> mRootList = new Dictionary<string, GameObject>();
        
        /// <summary>
        /// different toggle item for different block category
        /// </summary>
        protected Dictionary<string, Toggle> mMenuList = new Dictionary<string, Toggle>();

        protected Workspace mWorkspace;
        protected ToolboxConfig mConfig;

        protected abstract void Build();

        public void Init(Workspace workspace, ToolboxConfig config)
        {
            mWorkspace = workspace;
            mConfig = config;
            
            Build();
        }
        
        /// <summary>
        /// Create a new block view in toolbox 
        /// </summary>
        protected BlockView NewBlockView(string blockType, Transform parent, int index = -1)
        {
            Block block = mWorkspace.NewBlock(blockType);
            return NewBlockView(block, parent, index);
        }

        /// <summary>
        /// Create a new block view in toolbox 
        /// </summary>
        protected BlockView NewBlockView(Block block, Transform parent, int index = -1)
        {
            mWorkspace.RemoveTopBlock(block);
            
            BlockView view = BlockViewFactory.CreateView(block);
            view.InToolbox = true;
            view.ViewTransform.SetParent(parent, false);
            ToolboxBlockMask.AddMask(view);

            if (index >= 0)
                view.ViewTransform.SetSiblingIndex(index);
            
            return view;
        }
        
        #region Variables
        
        protected Dictionary<string, BlockView> mVariableGetterViews = new Dictionary<string, BlockView>();
        protected List<BlockView> mVariableHelperViews = new List<BlockView>();
        
        protected void BuildVariableBlocks()
        {
            Transform parent = mRootList[Define.BLOCK_CATEGORY_NAME_VARIABLE].transform;
            
            //build createVar button
            GameObject obj = GameObject.Instantiate(BlockViewSettings.Get().PrefabBtnCreateVar);
            obj.transform.SetParent(parent, false);
            obj.GetComponent<Button>().onClick.AddListener(() =>
            {
                DialogFactory.CreateDialog("variable_name");
            });

            List<VariableModel> allVars = mWorkspace.GetAllVariables();
            if (allVars.Count == 0) return;
            
            CreateVariableHelperViews();

            //list all variable getter views
            foreach (VariableModel variable in mWorkspace.GetAllVariables())
            {
                CreateVariableGetterView(variable.Name);
            }
        }

        protected void CreateVariableGetterView(string varName)
        {
            if (mVariableGetterViews.ContainsKey(varName))
                return;

            GameObject parentObj;
            if (!mRootList.TryGetValue(Define.BLOCK_CATEGORY_NAME_VARIABLE, out parentObj))
                return;

            Block block = mWorkspace.NewBlock(Define.VARIABLE_GET_BLOCK_TYPE);
            block.SetFieldValue("VAR", varName);
            BlockView view = NewBlockView(block, parentObj.transform);
            mVariableGetterViews[varName] = view;
        }

        protected void DeleteVariableGetterView(string varName)
        {
            BlockView view;
            mVariableGetterViews.TryGetValue(varName, out view);
            if (view != null)
            {
                mVariableGetterViews.Remove(varName);
                view.Dispose();
            }
        }
        
        protected void CreateVariableHelperViews()
        {
            GameObject parentObj;
            if (!mRootList.TryGetValue(Define.BLOCK_CATEGORY_NAME_VARIABLE, out parentObj))
                return;
            
            string varName = mWorkspace.GetAllVariables()[0].Name;
            List<string> blockTypes = BlockFactory.Instance.GetCategories()[Define.BLOCK_CATEGORY_NAME_VARIABLE];
            foreach (string blockType in blockTypes)
            {
                if (!blockType.Equals(Define.VARIABLE_GET_BLOCK_TYPE))
                {
                    Block block = mWorkspace.NewBlock(blockType);
                    block.SetFieldValue("VAR", varName);
                    BlockView view = NewBlockView(block, parentObj.transform);
                    mVariableHelperViews.Add(view);
                }
            }
        }

        protected void DeleteVariableHelperViews()
        {
            foreach (BlockView view in mVariableHelperViews)
            {
                view.Dispose();
            }
            mVariableHelperViews.Clear();
        }

        protected void OnVariableUpdate(VariableUpdateData updateData)
        {
            switch (updateData.Type)
            {
                case VariableUpdateData.Create:
                {
                    if (mVariableHelperViews.Count == 0)
                        CreateVariableHelperViews();
                    CreateVariableGetterView(updateData.VarName);
                    break;
                }
                case VariableUpdateData.Delete:
                {
                    DeleteVariableGetterView(updateData.VarName);

                    //change variable helper view
                    List<VariableModel> allVars = mWorkspace.GetAllVariables();
                    if (allVars.Count == 0)
                    {
                        DeleteVariableHelperViews();
                    }
                    else
                    {
                        foreach (BlockView view in mVariableHelperViews)
                        {
                            if (view.Block.GetFieldValue("VAR").Equals(updateData.VarName))
                            {
                                view.Block.SetFieldValue("VAR", allVars[0].Name);
                            }
                        }
                    }
                    break;
                }
                case VariableUpdateData.Rename:
                {
                    BlockView view;
                    mVariableGetterViews.TryGetValue(updateData.VarName, out view);
                    if (view != null)
                    {
                        mVariableGetterViews.Remove(updateData.VarName);
                        mVariableGetterViews[updateData.NewVarName] = view;
                    }
                    break;
                }
            }
        }

        private class VariableObserver : IObserver<VariableUpdateData>
        {
            private BaseToolbox mToolbox;

            public VariableObserver(BaseToolbox toolbox)
            {
                mToolbox = toolbox;
            }

            public void OnUpdated(object subject, VariableUpdateData args)
            {
                if (mToolbox == null || mToolbox.transform == null)
                    ((Observable<VariableUpdateData>) subject).RemoveObserver(this);
                else mToolbox.OnVariableUpdate(args);
            }
        }
        #endregion
        
        #region Procedures
        
        protected Dictionary<string, BlockView> mProcedureCallerViews = new Dictionary<string, BlockView>();
        
        protected void BuildProcedureBlocks()
        {
            Transform parent = mRootList[Define.BLOCK_CATEGORY_NAME_PROCEDURE].transform;
            List<string> blockTypes = BlockFactory.Instance.GetCategories()[Define.BLOCK_CATEGORY_NAME_PROCEDURE];
            foreach (string blockType in blockTypes)
            {
                if (!blockType.Equals(Define.CALL_NO_RETURN_BLOCK_TYPE) &&
                    !blockType.Equals(Define.CALL_WITH_RETURN_BLOCK_TYPE))
                {
                    NewBlockView(blockType, parent);
                }
            }
            
            // list all caller views
            foreach (Block block in mWorkspace.ProcedureDB.GetDefinitionBlocks())
            {
                CreateProcedureCallerView(((ProcedureDefinitionMutator) block.Mutator).ProcedureInfo, ProcedureDB.HasReturn(block));
            }
        }

        protected void CreateProcedureCallerView(Procedure procedureInfo, bool hasReturn)
        {
            if (mProcedureCallerViews.ContainsKey(procedureInfo.Name))
                return;
            
            GameObject parentObj;
            if (!mRootList.TryGetValue(Define.BLOCK_CATEGORY_NAME_PROCEDURE, out parentObj))
                return;

            string blockType = hasReturn ? Define.CALL_WITH_RETURN_BLOCK_TYPE : Define.CALL_NO_RETURN_BLOCK_TYPE;
            Block block = mWorkspace.NewBlock(blockType);
            block.SetFieldValue("NAME", procedureInfo.Name);
            BlockView view = NewBlockView(block, parentObj.transform);
            mProcedureCallerViews[procedureInfo.Name] = view;
        }
        
        protected void DeleteProcedureCallerView(Procedure procedureInfo)
        {
            BlockView view;
            mProcedureCallerViews.TryGetValue(procedureInfo.Name, out view);
            if (view != null)
            {
                mProcedureCallerViews.Remove(procedureInfo.Name);
                view.Dispose();
            }
        }
        
        protected void OnProcedureUpdate(ProcedureUpdateData updateData)
        {
            switch (updateData.Type)
            {
                case ProcedureUpdateData.Add:
                {
                    CreateProcedureCallerView(updateData.ProcedureInfo, ProcedureDB.HasReturn(updateData.ProcedureDefinitionBlock));
                    break;
                }
                case ProcedureUpdateData.Remove:
                {
                    DeleteProcedureCallerView(updateData.ProcedureInfo);
                    break;
                }
                case ProcedureUpdateData.Mutate:
                {
                    //mutate the caller prototype view
                    BlockView view = mProcedureCallerViews[updateData.ProcedureInfo.Name];
                    if (!updateData.ProcedureInfo.Name.Equals(updateData.NewProcedureInfo.Name))
                    {
                        mProcedureCallerViews.Remove(updateData.ProcedureInfo.Name);
                        mProcedureCallerViews[updateData.NewProcedureInfo.Name] = view;
                    }
                    
                    ((ProcedureMutator) view.Block.Mutator).Mutate(updateData.NewProcedureInfo);
                    break;
                }
            }
        }
        
        private class ProcedureObserver : IObserver<ProcedureUpdateData>
        {
            private BaseToolbox mToolbox;

            public ProcedureObserver(BaseToolbox toolbox)
            {
                mToolbox = toolbox;
            }

            public void OnUpdated(object subject, ProcedureUpdateData args)
            {
                if (mToolbox == null || mToolbox.transform == null)
                    ((Observable<ProcedureUpdateData>) subject).RemoveObserver(this);
                else mToolbox.OnProcedureUpdate(args);
            }
        }
        
        #endregion
    }
}
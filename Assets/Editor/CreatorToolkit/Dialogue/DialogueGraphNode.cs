using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Zhongshan.CreatorToolkit.Dialogue
{
    public class DialogueGraphNode : Node
    {
        public string GUID;
        public string NodeId;
        public string Speaker;
        public string Portrait;
        public string ContentText;
        
        // 存储实际的分支数据
        public List<global::DialogueChoice> Choices = new List<global::DialogueChoice>();
        
        public bool EntryPoint = false;
    }
}

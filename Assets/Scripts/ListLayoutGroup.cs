using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 纵向列表
/// </summary>
public class ListLayoutGroup : MonoBehaviour {
    [Flags]
    public enum LayoutType {
        Horizontal,
        Vertical
    }
    // ########## 外部基础参数设置 ##########
    //单元格宽
    private float m_fItemWidth;
    //单元格高
    private float m_fItemHeight;
    [Header ("Offset")]
    //列间隔
    public float m_fOffsetX = 0;
    //行间隔
    public float m_fOffsetY = 0;
    //类型
    [Header ("LayoutType")]
    public LayoutType m_LayoutType = LayoutType.Horizontal;

    [Header ("Column Or Row Count")]
    public int m_GroupCount = 0;

    [Header ("CellPrefab")]
    public GameObject m_Item;

    //复用个数
    private int m_nReuseCount = 0;
    //基础设置
    private ScrollRect m_ScrollRect;
    private RectTransform m_Content;

    //######### 根据参数来计算 ##########
    //创建的数量
    private int m_nCreateCount;
    //列表宽度
    private float m_fRectWidth;
    //列表高度
    private float m_fRectHeight;
    //列表总的需要显示的数量，外部给
    private int m_nListCount;
    //当前实际显示的数量(小于或等于createCount)
    private int m_nShowCount;
    //记录上次的初始序号
    private int m_nLastStartIndex = 0;
    //显示开始序号
    private int m_nStartIndex = 0;
    //显示结束序号
    private int m_nEndIndex = 0;
    //item对应的序号
    private Dictionary<int, Transform> m_ItemIndexDic = new Dictionary<int, Transform> ();
    private Vector3 m_CurItemParentPos = Vector3.zero;
    private Transform m_TempItem;

    public delegate void UpdateListItemEvent (Transform item, int index);
    private UpdateListItemEvent m_TempUpdateItem = null;

    /// <summary>
    /// 初始化列表 item长宽，列数和行 
    /// </summary>
    public void InitData () {
        m_ScrollRect = transform.GetComponent<ScrollRect> ();
        m_Content = m_ScrollRect.content;
        m_Content.pivot = new Vector2 (0, 1);
        //计算宽度
        m_fItemWidth = m_Item.GetComponent<RectTransform> ().rect.width;
        //计算高度
        m_fItemHeight = m_Item.GetComponent<RectTransform> ().rect.height;

        if (null != m_ScrollRect) {
            m_ScrollRect.onValueChanged.AddListener (OnValueChange);
        }
        //垂直
        if (m_LayoutType == LayoutType.Vertical) {
            m_nReuseCount = (int) (m_ScrollRect.viewport.rect.height / (m_fItemHeight + m_fOffsetY) + 1.5f);
            m_fRectWidth = m_GroupCount * (m_fItemWidth + m_fOffsetX) - m_fOffsetX;
        }
        //水平
        else {
            m_nReuseCount = (int) (m_ScrollRect.viewport.rect.width / (m_fItemWidth + m_fOffsetX) + 1.5f);
            m_fRectHeight = m_GroupCount * (m_fItemHeight + m_fOffsetY) - m_fOffsetY;
        }
        m_nCreateCount = m_GroupCount * m_nReuseCount;
        m_Item.SetActive (false);
        RectTransform itemRec = m_Item.GetComponent<RectTransform> ();
        itemRec.anchorMin = new Vector2 (0, 1);
        itemRec.anchorMax = new Vector2 (0, 1);
        itemRec.pivot = new Vector2 (0, 1);
    }

    /// <summary>
    /// 刷新赋值列表 回滚到顶部
    /// </summary>
    /// <param 列表的元素的最大个数="count"></param>
    /// <param 委托:进行 单个元素的赋值 = "updateItem"></param>
    public void InitList (int count, UpdateListItemEvent updateItem) {
        InitData ();
        //记录有多少个item
        m_nListCount = count;
        m_TempUpdateItem = updateItem;
        m_Content.transform.localPosition = Vector2.zero;

        int sideCount = count / m_GroupCount + (count % m_GroupCount > 0 ? 1 : 0);
        if (m_LayoutType == LayoutType.Vertical) {
            //计算有多少行，用于计算出总高度
            m_fRectHeight = Mathf.Max (0, sideCount * m_fItemHeight + (sideCount - 1) * m_fOffsetY);
        } else {
            //计算有多少列，用于计算出总高度
            m_fRectWidth = Mathf.Max (0, sideCount * m_fItemWidth + (sideCount - 1) * m_fOffsetX);
        }
        m_Content.sizeDelta = new Vector2 (m_fRectWidth, m_fRectHeight);
        //显示item的数量
        m_nShowCount = Mathf.Min (count, m_nCreateCount);
        m_nStartIndex = 0;
        m_ItemIndexDic.Clear ();

        for (int i = 0; i < m_nShowCount; i++) {
            Transform item = GetItem (i);
            SetItem (item, i);
        }
        //显示多少个
        ShowListCount (m_Content, m_nShowCount);
    }

    /// <summary>
    /// 创建item 有就拿来用，没有就创建
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    private Transform GetItem (int index) {
        Transform item = null;
        if (index < m_Content.childCount)
            item = m_Content.GetChild (index);
        else
            item = Instantiate (m_Item).transform;
        item.name = index.ToString ();
        item.SetParent (m_Content);
        item.localScale = Vector3.one;
        return item;
    }

    /// <summary>
    /// 刷新item对应数据信息
    /// </summary>
    /// <param name="item"></param>
    /// <param name="index"></param>
    private void SetItem (Transform item, int index) {
        m_ItemIndexDic[index] = item;
        item.localPosition = GetPos (index);
        item.name = index.ToString ();
        if (m_TempUpdateItem != null)
            m_TempUpdateItem (item, index);
    }

    /// <summary>
    /// item对应位置
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    private Vector2 GetPos (int index) {
        Vector2 retVec = Vector2.zero;
        if (m_LayoutType == LayoutType.Vertical) {
            retVec = new Vector2 (index % m_GroupCount * (m_fItemWidth + m_fOffsetX), -index / m_GroupCount * (m_fItemHeight + m_fOffsetY));
        } else {
            retVec = new Vector2 (index / m_GroupCount * (m_fItemWidth + m_fOffsetX), -index % m_GroupCount * (m_fItemHeight + m_fOffsetY));
        }
        return retVec;
    }

    /// <summary>
    /// 获取起始序列号
    /// </summary>
    /// <param name="y"></param>
    /// <returns></returns>
    private int GetStartIndex (float posxy) {
        posxy = Math.Abs (posxy);
        int retIndex = 0;
        if (m_LayoutType == LayoutType.Vertical && posxy <= m_fItemHeight) {
            return retIndex;
        } else if (m_LayoutType == LayoutType.Vertical && posxy <= m_fItemWidth) {
            return retIndex;
        }
        //拉到底部了
        if (IsScrollToBottom (posxy)) {
            if (m_nListCount <= m_nCreateCount)
                return 0;
            else
                return m_nListCount - m_nCreateCount;
        }
        if (m_LayoutType == LayoutType.Vertical) {
            retIndex = (int) ((posxy / (m_fItemHeight + m_fOffsetY)) + (posxy % (m_fItemHeight + m_fOffsetY) > 0 ? 1 : 0) - 1) * m_GroupCount;
        } else {
            retIndex = (int) ((posxy / (m_fItemWidth + m_fOffsetX)) + (posxy % (m_fItemWidth + m_fOffsetX) > 0 ? 1 : 0) - 1) * m_GroupCount;
        }
        return retIndex;
    }

    /// <summary>
    /// 是否滑动到底部
    /// </summary>
    /// <returns></returns>
    private bool IsScrollToBottom (float posxy) {
        bool retBool = false;
        if (m_LayoutType == LayoutType.Vertical) {
            retBool = posxy >= (m_Content.sizeDelta.y - m_ScrollRect.viewport.rect.height);
        } else {
            retBool = posxy >= (m_Content.sizeDelta.x - m_ScrollRect.viewport.rect.width);
        }
        return retBool;
    }

    /// <summary>
    /// 显示子物体的数量
    /// </summary>
    /// <param name="trans"></param>
    /// <param name="num"></param>
    private void ShowListCount (Transform trans, int num) {
        if (trans.childCount < num)
            return;
        for (int i = 0; i < num; i++) {
            trans.GetChild (i).gameObject.SetActive (true);
        }
        for (int i = num; i < trans.childCount; i++) {
            trans.GetChild (i).gameObject.SetActive (false);
        }
    }

    List<int> newIndexList = new List<int> ();
    List<int> changeIndexList = new List<int> ();

    /// <summary>
    /// 列表位置刷新
    /// </summary>
    /// <param name="pos"></param>
    private void OnValueChange (Vector2 pos) {
        m_CurItemParentPos = m_Content.localPosition;

        if (m_nListCount <= m_nCreateCount)
            return;
        if (m_LayoutType == LayoutType.Vertical) {
            m_nStartIndex = GetStartIndex (m_Content.localPosition.y);
        } else {
            m_nStartIndex = GetStartIndex (m_Content.localPosition.x);
        }

        //到底了
        if (m_nStartIndex + m_nCreateCount >= m_nListCount) {
            m_nStartIndex = m_nListCount - m_nCreateCount;
            m_nEndIndex = m_nListCount - 1;
        }
        //最后一个元素的下标
        else {
            m_nEndIndex = m_nStartIndex + m_nCreateCount - 1;
        }
        if (m_nStartIndex == m_nLastStartIndex)
            return;
        m_nLastStartIndex = m_nStartIndex;
        newIndexList.Clear ();
        changeIndexList.Clear ();
        for (int i = m_nStartIndex; i <= m_nEndIndex; i++) {
            newIndexList.Add (i);
        }
        var e = m_ItemIndexDic.GetEnumerator ();
        while (e.MoveNext ()) {
            int index = e.Current.Key;
            if (index >= m_nStartIndex && index <= m_nEndIndex) {
                if (newIndexList.Contains (index))
                    newIndexList.Remove (index);
                continue;
            } else {
                changeIndexList.Add (e.Current.Key);
            }
        }

        for (int i = 0; i < newIndexList.Count && i < changeIndexList.Count; i++) {
            int oldIndex = changeIndexList[i];
            int newIndex = newIndexList[i];
            if (newIndex >= 0 && newIndex < m_nListCount) {
                m_TempItem = m_ItemIndexDic[oldIndex];
                m_ItemIndexDic.Remove (oldIndex);
                SetItem (m_TempItem, newIndex);
            }
        }
    }

    /// <summary>
    /// 生成列表 不回滚,继续往下浏览
    /// </summary>
    /// <param 列表的元素的最大个数="count"></param>
    /// <param 委托:进行 单个元素的赋值 = "updateItem"></param>
    public void Refresh (int count, UpdateListItemEvent updateItem) {
        m_TempUpdateItem = updateItem;
        m_Content.sizeDelta = new Vector2 (m_fRectWidth, m_fRectHeight);
        m_nListCount = count;
        m_nShowCount = Mathf.Min (count, m_nCreateCount); //显示item的数量
        m_ItemIndexDic.Clear ();
        if (count == 0) {
            ShowListCount (m_Content, m_nShowCount);
            return;
        }
        //计算起始的终止序号
        //--如果数量小于遮罩正常状态下能显示的总量
        if (count <= m_nCreateCount) {
            m_nStartIndex = 0;
            m_nEndIndex = count - 1;
        } else {
            if (m_LayoutType == LayoutType.Vertical) {
                m_nStartIndex = GetStartIndex (m_Content.localPosition.y);
            } else {
                m_nStartIndex = GetStartIndex (m_Content.localPosition.x);
            }
            if (m_nStartIndex + m_nCreateCount >= count) {
                m_nStartIndex = count - m_nCreateCount;
                m_nEndIndex = count - 1;
            } else {
                m_nEndIndex = m_nStartIndex + m_nCreateCount - 1;
            }
        }
        m_nLastStartIndex = m_nStartIndex;
        if (m_nEndIndex < m_nStartIndex) {
            Debug.LogError ("列表有问题！");
            return;
        }
        for (int i = m_nStartIndex; i <= m_nEndIndex; i++) {
            Transform item = GetItem (i - m_nStartIndex);
            SetItem (item, i);
        }
        ShowListCount (m_Content, m_nShowCount);
    }
}
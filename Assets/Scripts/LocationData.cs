using UnityEngine;
using System;

// ========== 地点 ID 枚举 ==========

/// <summary>
/// 校园地点唯一标识
/// </summary>
public enum LocationId
{
    Dormitory,        // 宿舍
    TeachingBuilding, // 教学楼
    Library,          // 图书馆
    Canteen,          // 食堂
    Playground,       // 操场
    Store,            // 教超
    ExpressStation,   // 快递站
    TakeoutStation    // 外卖站
}

// ========== 地点定义 ==========

/// <summary>
/// 单个地点的完整定义数据
/// </summary>
[System.Serializable]
public class LocationDefinition
{
    public LocationId id;
    public string displayName;           // "宿舍"
    public string description;           // "你的大学宿舍，温馨的小窝"
    public string iconChar;              // 地图上的文字图标，如 "🏠"
    public Vector2 mapPosition;          // 地图 UI 节点坐标（归一化 0-1）
    public string[] availableActionIds;  // 该地点可用的行动 id 列表
    public LocationId[] adjacentLocations; // 相邻地点（移动 0AP）
    public string openTimeDesc;          // 开放时间描述

    public LocationDefinition(
        LocationId id,
        string displayName,
        string description,
        string iconChar,
        Vector2 mapPosition,
        string[] availableActionIds,
        LocationId[] adjacentLocations,
        string openTimeDesc = "全天开放")
    {
        this.id = id;
        this.displayName = displayName;
        this.description = description;
        this.iconChar = iconChar;
        this.mapPosition = mapPosition;
        this.availableActionIds = availableActionIds;
        this.adjacentLocations = adjacentLocations;
        this.openTimeDesc = openTimeDesc;
    }
}

// ========== 地点连接 ==========

/// <summary>
/// 两个地点之间的连接关系（用于地图连线绘制）
/// </summary>
[System.Serializable]
public class LocationLink
{
    public LocationId from;
    public LocationId to;

    public LocationLink(LocationId from, LocationId to)
    {
        this.from = from;
        this.to = to;
    }
}

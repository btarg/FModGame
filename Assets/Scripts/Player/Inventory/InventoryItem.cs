﻿using System;
using BattleSystem.ScriptableObjects.Skills;
using BattleSystem.ScriptableObjects.Stats.Modifiers;
using UnityEngine;

namespace Player.Inventory
{
    public enum ItemType {
        Weapon,
        Wearable,
        ConsumableSkill,
        ConsumableBuffDebuff,
        QuestItem,
        KeyItem
    }
    
    public class InventoryItem : ScriptableObject
    {
        public string displayName;
        public string description;
        public ItemType itemType;
        public BuffDebuff buffDebuff;
        // if it's a weapon, it will have a Skill associated with it
        public BaseSkill skill;
        public int value;
    }
}
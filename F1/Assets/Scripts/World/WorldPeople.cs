using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>D-289 World 층: 주민 데이터(이름·자리·역할 한 줄). 원천 = world/world.yaml. 사용처는 아직 없다(1단계).</summary>
public class WorldPeople : ScriptableObject
{
    [Serializable]
    public class Person
    {
        public string name;
        public string place;
        public string role;
        /// <summary>확정 | 가안(도현 판정 대기).</summary>
        public string status;
    }

    public List<Person> people = new List<Person>();
}

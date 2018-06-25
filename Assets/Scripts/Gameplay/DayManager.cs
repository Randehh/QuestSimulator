﻿using Rondo.Generic.Utility;
using Rondo.QuestSim.Heroes;
using Rondo.QuestSim.Inventory;
using Rondo.QuestSim.Quests;
using Rondo.QuestSim.UI.General;
using Rondo.QuestSim.UI.PostedQuests;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rondo.QuestSim.Gameplay {

    public class DayManager : MonoBehaviourSingleton<DayManager> {

        public int CurrentDay { get; set; }
        public Action OnNextDay = delegate { };

        private List<QuestInstance> m_ActiveQuestsToUpdate = new List<QuestInstance>();
        private List<QuestInstance> m_QuestsToAssign = new List<QuestInstance>();

        private void Awake() {
            CurrentDay = 1;

            Instance = this;
        }

        public void EndDay() {
            QuestDetailsWindow.Instance.OnWindowClose += NextDayStep;

            m_QuestsToAssign = new List<QuestInstance>(QuestManager.PostedQuests);
            m_ActiveQuestsToUpdate = new List<QuestInstance>(QuestManager.ActiveQuests.Keys);

            NextDayStep();
        }

        private void NextDayStep() {
            if(m_QuestsToAssign.Count != 0) {
                QuestInstance nextQuest = m_QuestsToAssign[m_QuestsToAssign.Count - 1];
                m_QuestsToAssign.Remove(nextQuest);

                QuestDetailsWindow.Instance.OpenWindow(nextQuest, QuestDetailsWindow.QuestMode.HERO_SELECT);
                return;
            }

            while (m_ActiveQuestsToUpdate.Count != 0) {
                //Show reward thing, for now just give everything
                QuestInstance activeQuest = m_ActiveQuestsToUpdate[m_ActiveQuestsToUpdate.Count -1];
                activeQuest.DaysLeftOnQuest--;

                m_ActiveQuestsToUpdate.Remove(activeQuest);

                if (activeQuest.DaysLeftOnQuest <= 0) {
                    QuestDetailsWindow.Instance.OpenWindow(activeQuest, QuestDetailsWindow.QuestMode.COMPLETED);
                    activeQuest.CompleteQuest(QuestManager.ActiveQuests[activeQuest]);
                    return;
                }
            }

            CurrentDay++;

            OnNextDay();

            QuestManager.PostedQuests = UpdateQuestTimeLimits(QuestManager.PostedQuests, 0);
            QuestManager.Requests = UpdateQuestTimeLimits(QuestManager.Requests, 1);
            InventoryManager.Gold -= 10;

            NightFadeUI.Instance.Disable(()=> { });
            QuestDetailsWindow.Instance.OnWindowClose -= NextDayStep;
        }

        private List<QuestInstance> UpdateQuestTimeLimits(List<QuestInstance> list, int dayLimit) {
            for (int i = list.Count - 1; i >= 0; i--) {
                QuestInstance quest = list[i];
                quest.DaysLeftOnPost--;
                if (quest.DaysLeftOnPost <= dayLimit) {
                    list.Remove(quest);
                }
            }
            return list;
        }

    }

}
﻿using Rondo.QuestSim.Inventory;
using Rondo.QuestSim.Quests;
using Rondo.QuestSim.Quests.Rewards;
using Rondo.QuestSim.Reputation;
using Rondo.QuestSim.UI.General;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rondo.QuestSim.UI.Requests {

    public class RequestPostWindow : MonoBehaviour {

        public TextMeshProUGUI questChainTitle;
        public TextMeshProUGUI difficultyText;

        public TMP_InputField goldInputField;
        public TMP_Dropdown itemDropdown;
        public Button addItemButton;
        public GameObject addedItemTemplate;

        public Button cancelButton;
        public Button postButton;

        private QuestInstance m_CurrentRequest;
        private int m_ItemDropdownSelected = 0;
        private List<ItemEntry> m_ItemEntries = new List<ItemEntry>();

        void Start() {
            cancelButton.onClick.AddListener(() => {
                Reset(true);
                gameObject.SetActive(false);
            });

            postButton.onClick.AddListener(() => {
                foreach(ItemEntry itemReward in m_ItemEntries) {
                    m_CurrentRequest.ItemRewards.Add(new QuestRewardItem(itemReward.item));
                }

                Reset(false);
                gameObject.SetActive(false);

                QuestManager.Requests.Remove(m_CurrentRequest);
                QuestManager.PostedQuests.Add(m_CurrentRequest);

                RequestsWindow.Instance.ReloadInstances();
            });

            goldInputField.contentType = TMP_InputField.ContentType.IntegerNumber;
            goldInputField.onEndEdit.AddListener((value) => {
                if (string.IsNullOrEmpty(value)) {
                    value = "0";
                    goldInputField.text = value;
                }
                int iValue = int.Parse(value);
                m_CurrentRequest.GoldReward.GoldCount = iValue;
            });

            addItemButton.onClick.AddListener(CreateItemEntry);

            itemDropdown.onValueChanged.AddListener((value) => {
                m_ItemDropdownSelected = value;

                GameItemPopupCaller[] itemCallers = itemDropdown.GetComponentsInChildren<GameItemPopupCaller>();
                for (int i = 0; i < itemCallers.Length; i++) {
                    if (i == 0) continue;
                    itemCallers[i].enabled = false;
                }
            });
        }

        public void OpenWindow(QuestInstance request) {
            if(request == m_CurrentRequest) {
                gameObject.SetActive(!gameObject.activeSelf);
            } else {
                Reset(true);
                gameObject.SetActive(true);
            }
            m_CurrentRequest = request;

            questChainTitle.text = "<b><u>" + request.QuestSource.RequestTitle + "</u></b>\n<size=18><i>" + request.ObjectiveCount + " Objective(s)</i></size>";
            difficultyText.text = ""+request.DifficultyLevel;
        }

        private void CreateItemEntry() {
            if (m_ItemDropdownSelected == 0) return;

            GameObject entryObj = Instantiate(addedItemTemplate);
            entryObj.transform.SetParent(addedItemTemplate.transform.parent);
            entryObj.SetActive(true);
            addItemButton.transform.parent.SetAsLastSibling();
            GameItem item = InventoryManager.OwnedItems[m_ItemDropdownSelected - 1];
            InventoryManager.MoveItemToReserved(item);

            ItemEntry newEntry = new ItemEntry(entryObj, item, DeleteItemEntry);
            m_ItemEntries.Add(newEntry);

            UpdateItemDropdown();
            GameItemPopup.Instance.SwitchItemTarget(null);
        }

        private void DeleteItemEntry(ItemEntry entry, bool reclaimItem) {
            if (!m_ItemEntries.Contains(entry)) return;
            Destroy(entry.parent);
            if(reclaimItem) InventoryManager.MoveItemToOwned(entry.item);
            m_ItemEntries.Remove(entry);

            UpdateItemDropdown();
        }

        private void UpdateItemDropdown() {
            itemDropdown.ClearOptions();

            List<string> dropdownOptions = new List<string> { "-" };
            foreach(GameItem item in InventoryManager.OwnedItems) {
                dropdownOptions.Add(item.DisplayName + " ("+ item.OverallPower + ")");
            }
            itemDropdown.AddOptions(dropdownOptions);
            itemDropdown.value = 0;
            itemDropdown.RefreshShownValue();
        }

        public void OnItemDropdownClick() {
            GameItemPopupCaller[] itemCallers = itemDropdown.GetComponentsInChildren<GameItemPopupCaller>();
            for (int i = 0; i < itemCallers.Length; i++) {
                if (i == 0) continue;
                itemCallers[i].associatedItem = InventoryManager.OwnedItems[i - 1];
            }
        }

        public void Reset(bool reclaimItems) {
            goldInputField.text = "0";
            m_ItemDropdownSelected = 0;
            
            for(int i = m_ItemEntries.Count - 1; i >= 0; i--) {
                ItemEntry itemEntry = m_ItemEntries[i];
                DeleteItemEntry(itemEntry, reclaimItems);
            }
            m_ItemEntries.Clear();

            UpdateItemDropdown();
        }

        private class ItemEntry {
            public GameObject parent;
            public GameItem item;
            private TextMeshProUGUI title;
            private Button button;

            public ItemEntry(GameObject parent, GameItem item, Action<ItemEntry, bool> OnDelete) {
                this.parent = parent;
                this.item = item;

                title = parent.GetComponentInChildren<TextMeshProUGUI>(true);
                button = parent.GetComponentInChildren<Button>(true);

                title.text = item.DisplayName;
                button.onClick.AddListener(() => {
                    OnDelete(this, true);
                });
            }
        }
    }

}
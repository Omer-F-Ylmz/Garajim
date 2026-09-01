(function () {
    "use strict";

    var TOKEN_KEY = "garajim_token";
    var USER_KEY = "garajim_user";

    var state = {
        token: null,
        user: null,
        vehicles: [],
        selectedVehicleId: null,
        documentRecordId: null,
        receiptDraft: null,
        chart: null
    };

    var TEAM_ROLES = [
        ["Manager", "Yönetici"],
        ["Driver", "Sürücü"],
        ["Owner", "Sahip"]
    ];

    var MAINTENANCE_TYPES = [
        ["PeriyodikBakim", "Periyodik bakım"],
        ["YagDegisimi", "Yağ değişimi"],
        ["FrenBakimi", "Fren bakımı"],
        ["LastikDegisimi", "Lastik değişimi"],
        ["AkuDegisimi", "Akü değişimi"],
        ["Diger", "Diğer"]
    ];

    var FUEL_TYPES = [
        ["Benzin", "Benzin"],
        ["Dizel", "Dizel"],
        ["Lpg", "LPG"],
        ["Hibrit", "Hibrit"],
        ["Elektrik", "Elektrik"]
    ];

    var EXPENSE_CATEGORIES = [
        ["TrafikSigortasi", "Trafik sigortası"],
        ["Kasko", "Kasko"],
        ["Mtv", "MTV"],
        ["Muayene", "Muayene"],
        ["EgzozEmisyon", "Egzoz emisyon"],
        ["Otopark", "Otopark"],
        ["Kopru", "Köprü / otoyol"],
        ["TrafikCezasi", "Trafik cezası"],
        ["Yikama", "Yıkama"],
        ["Diger", "Diğer"]
    ];

    var REMINDER_TYPES = [
        ["Muayene", "Muayene"],
        ["TrafikSigortasi", "Trafik sigortası"],
        ["Kasko", "Kasko"],
        ["EgzozEmisyon", "Egzoz emisyon"],
        ["Mtv", "MTV"],
        ["PeriyodikBakim", "Periyodik bakım"],
        ["LastikDegisimi", "Lastik değişimi"],
        ["Diger", "Diğer"]
    ];

    var PRICE_FUEL = ["Benzin", "Dizel", "LPG & Benzin", "Hibrit", "Elektrik"];
    var PRICE_GEAR = ["Düz", "Otomatik", "Yarı Otomatik"];
    var PRICE_BODY = ["Sedan", "Hatchback/5", "Hatchback/3", "Station wagon", "MPV", "Coupe", "SUV", "Cabrio", "Roadster"];

    var moneyFormat = new Intl.NumberFormat("tr-TR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    var wholeFormat = new Intl.NumberFormat("tr-TR", { maximumFractionDigits: 0 });
    var literFormat = new Intl.NumberFormat("tr-TR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

    function el(id) {
        return document.getElementById(id);
    }

    function clear(node) {
        while (node.firstChild) {
            node.removeChild(node.firstChild);
        }
    }

    function make(tag, text, className) {
        var node = document.createElement(tag);
        if (text !== undefined && text !== null) {
            node.textContent = String(text);
        }
        if (className) {
            node.className = className;
        }
        return node;
    }

    function labelOf(pairs, value) {
        for (var i = 0; i < pairs.length; i++) {
            if (pairs[i][0] === value) {
                return pairs[i][1];
            }
        }
        return value === null || value === undefined ? "" : String(value);
    }

    function fillSelect(select, pairs) {
        clear(select);
        pairs.forEach(function (pair) {
            var option = document.createElement("option");
            option.value = pair[0];
            option.textContent = pair[1];
            select.appendChild(option);
        });
    }

    function fillSimpleSelect(select, values) {
        fillSelect(select, values.map(function (value) { return [value, value]; }));
    }

    function money(value) {
        var number = Number(value);
        if (!isFinite(number)) {
            return "-";
        }
        return moneyFormat.format(number) + " TL";
    }

    function km(value) {
        var number = Number(value);
        if (!isFinite(number)) {
            return "-";
        }
        return wholeFormat.format(number) + " km";
    }

    function formatDate(value) {
        if (!value) {
            return "-";
        }
        var date = new Date(value);
        if (isNaN(date.getTime())) {
            return "-";
        }
        return date.toLocaleDateString("tr-TR");
    }

    function todayInput() {
        var now = new Date();
        var month = String(now.getMonth() + 1).padStart(2, "0");
        var day = String(now.getDate()).padStart(2, "0");
        return now.getFullYear() + "-" + month + "-" + day;
    }

    function showMessage(node, text, isOk) {
        node.textContent = text || "";
        node.className = isOk ? "message ok" : "message";
    }

    function clearMessages() {
        showMessage(el("auth-message"), "");
        showMessage(el("app-message"), "");
    }

    function saveSession(token, user) {
        state.token = token;
        state.user = user;
        localStorage.setItem(TOKEN_KEY, token);
        localStorage.setItem(USER_KEY, JSON.stringify(user));
    }

    function clearSession() {
        state.token = null;
        state.user = null;
        state.vehicles = [];
        state.selectedVehicleId = null;
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(USER_KEY);
    }

    function readSession() {
        var token = localStorage.getItem(TOKEN_KEY);
        if (!token) {
            return false;
        }
        state.token = token;
        try {
            state.user = JSON.parse(localStorage.getItem(USER_KEY) || "null");
        } catch (error) {
            state.user = null;
        }
        if (!state.user || !state.user.role) {
            clearSession();
            return false;
        }
        return true;
    }

    function goToLogin(message) {
        clearSession();
        if (state.chart) {
            state.chart.destroy();
            state.chart = null;
        }
        el("app-screen").classList.add("hidden");
        el("auth-screen").classList.remove("hidden");
        showMessage(el("auth-message"), message || "");
    }

    function api(path, options) {
        var settings = options || {};
        var headers = { "Accept": "application/json" };
        if (state.token) {
            headers.Authorization = "Bearer " + state.token;
        }
        var init = { method: settings.method || "GET", headers: headers };
        if (settings.body !== undefined) {
            if (typeof FormData !== "undefined" && settings.body instanceof FormData) {
                init.body = settings.body;
            } else {
                headers["Content-Type"] = "application/json";
                init.body = JSON.stringify(settings.body);
            }
        }

        var isAuthCall = path.indexOf("/api/Auth/") === 0;

        return fetch(path, init).then(function (response) {
            if (response.status === 401 && !isAuthCall) {
                goToLogin("Oturum süreniz doldu, lütfen tekrar giriş yapın.");
                throw new Error("Oturum gerekli.");
            }
            return response.text().then(function (text) {
                var payload = null;
                if (text) {
                    try {
                        payload = JSON.parse(text);
                    } catch (error) {
                        payload = null;
                    }
                }
                if (payload && payload.success === false) {
                    throw new Error(payload.message || "İşlem başarısız.");
                }
                if (!response.ok) {
                    throw new Error(readProblem(payload) || "Sunucu hatası (" + response.status + ").");
                }
                return payload;
            });
        });
    }

    function readProblem(payload) {
        if (!payload) {
            return null;
        }
        if (payload.errors) {
            var keys = Object.keys(payload.errors);
            if (keys.length > 0) {
                var first = payload.errors[keys[0]];
                if (Array.isArray(first) && first.length > 0) {
                    return "Geçersiz alan: " + keys[0];
                }
            }
        }
        return payload.title || payload.message || null;
    }

    function handleError(node, error) {
        var text = error && error.message ? error.message : "Beklenmeyen bir hata oluştu.";
        if (text === "Oturum gerekli.") {
            return;
        }
        if (text === "Failed to fetch") {
            text = "Sunucuya ulaşılamadı.";
        }
        showMessage(node, text);
    }

    function switchAuthTab(showLogin) {
        el("tab-login").classList.toggle("active", showLogin);
        el("tab-register").classList.toggle("active", !showLogin);
        el("login-form").classList.toggle("hidden", !showLogin);
        el("register-form").classList.toggle("hidden", showLogin);
        showMessage(el("auth-message"), "");
    }

    function currentRole() {
        return (state.user && state.user.role) || "Owner";
    }

    function isOwner() {
        return currentRole() === "Owner";
    }

    function canManage() {
        var role = currentRole();
        return role === "Owner" || role === "Manager";
    }

    function applyRole() {
        el("add-vehicle-btn").classList.toggle("hidden", !canManage());
        el("team-btn").classList.toggle("hidden", !isOwner());
        el("karne-btn").classList.toggle("hidden", !canManage());

        var zimmetTab = document.querySelector('.tab-btn[data-manager-only="true"]');
        if (zimmetTab) {
            zimmetTab.classList.toggle("hidden", !canManage());
            if (!canManage() && zimmetTab.classList.contains("active")) {
                selectTab("bakim");
            }
        }
        if (!isOwner()) {
            el("team-box").classList.add("hidden");
        }
        if (!canManage()) {
            el("karne-box").classList.add("hidden");
        }
    }

    function enterApp() {
        el("auth-screen").classList.add("hidden");
        el("app-screen").classList.remove("hidden");
        var user = state.user || {};
        var label = user.fullName || "";
        if (user.companyName) {
            label = label ? label + " · " + user.companyName : user.companyName;
        }
        el("user-label").textContent = label;
        applyRole();
        loadVehicles();
        loadPendingReceipts();
    }

    function loadVehicles() {
        return api("/api/Vehicles").then(function (result) {
            state.vehicles = (result && result.data) || [];
            var select = el("vehicle-select");
            clear(select);

            state.vehicles.forEach(function (vehicle) {
                var option = document.createElement("option");
                option.value = String(vehicle.id);
                option.textContent = vehicle.plate + " - " + vehicle.brand + " " + vehicle.model;
                select.appendChild(option);
            });

            var hasVehicles = state.vehicles.length > 0;
            renderEmptyState(hasVehicles);
            el("empty-state").classList.toggle("hidden", hasVehicles);
            el("workspace").classList.toggle("hidden", !hasVehicles);
            select.classList.toggle("hidden", !hasVehicles);

            if (!hasVehicles) {
                state.selectedVehicleId = null;
                return;
            }

            var stillThere = state.vehicles.some(function (vehicle) {
                return vehicle.id === state.selectedVehicleId;
            });
            if (!stillThere) {
                state.selectedVehicleId = state.vehicles[0].id;
            }
            select.value = String(state.selectedVehicleId);
            loadActiveTab();
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function renderEmptyState(hasVehicles) {
        if (hasVehicles) {
            return;
        }
        if (canManage()) {
            el("empty-title").textContent = "Henüz aracınız yok";
            el("empty-text").textContent = "Kayıt tutmaya başlamak için üstteki + Araç düğmesiyle bir araç ekleyin.";
        } else {
            el("empty-title").textContent = "Size zimmetli araç yok";
            el("empty-text").textContent = "Bir araç zimmetlendiğinde kayıtları burada görürsünüz. Zimmet için şirket yöneticinize başvurun.";
        }
    }

    function activeTab() {
        var button = document.querySelector(".tab-btn.active");
        return button ? button.getAttribute("data-tab") : "bakim";
    }

    function loadActiveTab() {
        if (!state.selectedVehicleId) {
            return;
        }
        var tab = activeTab();
        if (tab === "bakim") {
            loadMaintenance();
        } else if (tab === "yakit") {
            loadFuel();
        } else if (tab === "masraf") {
            loadExpenses();
        } else if (tab === "hatirlatma") {
            loadReminders();
        } else if (tab === "rapor") {
            loadFuelStats();
            loadMonthly();
        } else if (tab === "zimmet") {
            loadAssignments();
        } else if (tab === "parca") {
            loadPartMemory();
        }
    }

    function emptyRow(tbody, columns, text) {
        var row = document.createElement("tr");
        row.className = "empty-row";
        var cell = make("td", text);
        cell.colSpan = columns;
        row.appendChild(cell);
        tbody.appendChild(row);
    }

    function deleteButton(onClick) {
        var cell = document.createElement("td");
        var button = make("button", "Sil", "link-btn");
        button.type = "button";
        button.addEventListener("click", onClick);
        cell.appendChild(button);
        return cell;
    }

    function loadMaintenance() {
        var tbody = el("maintenance-rows");
        api("/api/Maintenance?vehicleId=" + state.selectedVehicleId).then(function (result) {
            var rows = (result && result.data) || [];
            clear(tbody);
            if (rows.length === 0) {
                emptyRow(tbody, 7, "Kayıt yok.");
                return;
            }
            rows.forEach(function (item) {
                var tr = document.createElement("tr");
                tr.appendChild(make("td", formatDate(item.date)));
                tr.appendChild(make("td", labelOf(MAINTENANCE_TYPES, item.type)));
                tr.appendChild(make("td", km(item.km)));
                tr.appendChild(make("td", money(item.cost)));
                tr.appendChild(make("td", item.serviceName || "-"));
                tr.appendChild(documentButton(item.id));
                tr.appendChild(deleteButton(function () { removeRecord("/api/Maintenance/" + item.id, loadMaintenance); }));
                tbody.appendChild(tr);
            });
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function documentButton(recordId) {
        var cell = document.createElement("td");
        var button = make("button", "Belgeler", "link-btn");
        button.type = "button";
        button.addEventListener("click", function () { openDocuments(recordId); });
        cell.appendChild(button);
        return cell;
    }

    function openDocuments(recordId) {
        state.documentRecordId = recordId;
        el("document-box").classList.remove("hidden");
        el("document-title").textContent = "Bakım kaydı #" + recordId + " belgeleri";
        el("document-form").reset();
        loadDocuments();
    }

    function closeDocuments() {
        state.documentRecordId = null;
        el("document-box").classList.add("hidden");
    }

    function loadDocuments() {
        var list = el("document-list");
        if (!state.documentRecordId) {
            clear(list);
            return;
        }
        api("/api/Documents?maintenanceRecordId=" + state.documentRecordId).then(function (result) {
            var rows = (result && result.data) || [];
            clear(list);
            if (rows.length === 0) {
                list.appendChild(make("li", "Bu kayda bağlı belge yok."));
                return;
            }
            rows.forEach(function (item) {
                var li = document.createElement("li");
                li.appendChild(make("span", item.originalName + " · " + fileSize(item.sizeBytes)));

                var actions = make("span", "", "row-actions");
                var download = make("button", "İndir", "link-btn");
                download.type = "button";
                download.addEventListener("click", function () { downloadDocument(item); });
                actions.appendChild(download);

                var remove = make("button", "Sil", "link-btn");
                remove.type = "button";
                remove.addEventListener("click", function () { removeDocument(item.id); });
                actions.appendChild(remove);

                li.appendChild(actions);
                list.appendChild(li);
            });
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function fileSize(bytes) {
        var value = Number(bytes) || 0;
        if (value < 1024) {
            return value + " B";
        }
        if (value < 1024 * 1024) {
            return Math.round(value / 1024) + " KB";
        }
        return (value / (1024 * 1024)).toFixed(1) + " MB";
    }

    function downloadDocument(item) {
        var headers = state.token ? { Authorization: "Bearer " + state.token } : {};
        fetch("/api/Documents/" + item.id + "/download", { headers: headers }).then(function (response) {
            if (!response.ok) {
                throw new Error("Belge indirilemedi.");
            }
            return response.blob();
        }).then(function (blob) {
            var url = URL.createObjectURL(blob);
            var link = document.createElement("a");
            link.href = url;
            link.download = item.originalName;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            URL.revokeObjectURL(url);
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function removeDocument(id) {
        api("/api/Documents/" + id, { method: "DELETE" }).then(function (result) {
            showMessage(el("app-message"), (result && result.message) || "Belge silindi.", true);
            loadDocuments();
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function loadTeam() {
        var tbody = el("team-rows");
        api("/api/Team").then(function (result) {
            var rows = (result && result.data) || [];
            clear(tbody);
            if (rows.length === 0) {
                emptyRow(tbody, 5, "Kayıt yok.");
                return;
            }
            rows.forEach(function (item) {
                var tr = document.createElement("tr");
                tr.appendChild(make("td", item.fullName));
                tr.appendChild(make("td", item.email));

                var roleCell = document.createElement("td");
                if (state.user && item.email === state.user.email) {
                    roleCell.textContent = labelOf(TEAM_ROLES, item.role);
                } else {
                    var select = document.createElement("select");
                    fillSelect(select, TEAM_ROLES);
                    select.value = item.role;
                    select.addEventListener("change", function () { changeRole(item.id, select.value); });
                    roleCell.appendChild(select);
                }
                tr.appendChild(roleCell);
                tr.appendChild(make("td", item.isActive ? "Aktif" : "Pasif"));

                var actionCell = document.createElement("td");
                var kendisi = state.user && item.email === state.user.email;
                if (item.isActive && !kendisi) {
                    var button = make("button", "Pasifleştir", "link-btn");
                    button.type = "button";
                    button.addEventListener("click", function () { deactivateMember(item.id); });
                    actionCell.appendChild(button);
                }
                tr.appendChild(actionCell);
                tbody.appendChild(tr);
            });
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function changeRole(id, role) {
        api("/api/Team/" + id + "/role", { method: "PUT", body: { role: role } }).then(function (result) {
            showMessage(el("app-message"), (result && result.message) || "Rol güncellendi.", true);
            loadTeam();
        }).catch(function (error) {
            handleError(el("app-message"), error);
            loadTeam();
        });
    }

    function deactivateMember(id) {
        api("/api/Team/" + id + "/deactivate", { method: "PUT" }).then(function (result) {
            showMessage(el("app-message"), (result && result.message) || "Üye pasifleştirildi.", true);
            loadTeam();
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function loadAssignments() {
        if (!state.selectedVehicleId) {
            return;
        }
        var tbody = el("assignment-rows");
        api("/api/Assignments?vehicleId=" + state.selectedVehicleId).then(function (result) {
            var rows = (result && result.data) || [];
            clear(tbody);

            var aktif = null;
            rows.forEach(function (item) {
                if (item.isActive) {
                    aktif = item;
                }
            });
            el("assignment-current").textContent = aktif
                ? "Şu an zimmetli: " + aktif.userFullName + " (" + formatDate(aktif.startDate) + " tarihinden beri)"
                : "Bu araç şu an kimseye zimmetli değil.";
            el("assignment-submit").textContent = aktif ? "Devret" : "Zimmetle";
            el("assignment-end").classList.toggle("hidden", !aktif);

            if (rows.length === 0) {
                emptyRow(tbody, 3, "Zimmet geçmişi yok.");
            } else {
                rows.forEach(function (item) {
                    var tr = document.createElement("tr");
                    tr.appendChild(make("td", item.userFullName));
                    tr.appendChild(make("td", formatDate(item.startDate)));
                    tr.appendChild(make("td", item.endDate ? formatDate(item.endDate) : "Devam ediyor"));
                    tbody.appendChild(tr);
                });
            }

            return loadAssignableUsers();
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function loadAssignableUsers() {
        return api("/api/Team").then(function (result) {
            var rows = (result && result.data) || [];
            var select = el("assignment-user");
            clear(select);
            rows.filter(function (item) {
                return item.isActive;
            }).forEach(function (item) {
                var option = document.createElement("option");
                option.value = String(item.id);
                option.textContent = item.fullName + " (" + labelOf(TEAM_ROLES, item.role) + ")";
                select.appendChild(option);
            });
        });
    }

    function loadFuel() {
        var tbody = el("fuel-rows");
        api("/api/Fuel?vehicleId=" + state.selectedVehicleId).then(function (result) {
            var rows = (result && result.data) || [];
            clear(tbody);
            if (rows.length === 0) {
                emptyRow(tbody, 5, "Kayıt yok.");
                return;
            }
            rows.forEach(function (item) {
                var tr = document.createElement("tr");
                tr.appendChild(make("td", formatDate(item.date)));
                tr.appendChild(make("td", km(item.km)));
                tr.appendChild(make("td", literFormat.format(Number(item.liters)) + " L"));
                tr.appendChild(make("td", money(item.totalCost)));
                tr.appendChild(deleteButton(function () { removeRecord("/api/Fuel/" + item.id, loadFuel); }));
                tbody.appendChild(tr);
            });
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function loadExpenses() {
        var tbody = el("expense-rows");
        api("/api/Expenses?vehicleId=" + state.selectedVehicleId).then(function (result) {
            var rows = (result && result.data) || [];
            clear(tbody);
            if (rows.length === 0) {
                emptyRow(tbody, 5, "Kayıt yok.");
                return;
            }
            rows.forEach(function (item) {
                var tr = document.createElement("tr");
                tr.appendChild(make("td", formatDate(item.date)));
                tr.appendChild(make("td", labelOf(EXPENSE_CATEGORIES, item.category)));
                tr.appendChild(make("td", money(item.amount)));
                tr.appendChild(make("td", item.note || "-"));
                tr.appendChild(deleteButton(function () { removeRecord("/api/Expenses/" + item.id, loadExpenses); }));
                tbody.appendChild(tr);
            });
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function loadReminders() {
        var tbody = el("reminder-rows");
        api("/api/Reminders?vehicleId=" + state.selectedVehicleId).then(function (result) {
            var rows = (result && result.data) || [];
            clear(tbody);
            if (rows.length === 0) {
                emptyRow(tbody, 5, "Hatırlatma yok.");
                return;
            }
            rows.forEach(function (item) {
                var tr = document.createElement("tr");
                tr.appendChild(make("td", labelOf(REMINDER_TYPES, item.type)));
                tr.appendChild(make("td", item.dueDate ? formatDate(item.dueDate) : "-"));
                tr.appendChild(make("td", item.dueKm ? km(item.dueKm) : "-"));

                var statusCell = document.createElement("td");
                statusCell.appendChild(make("span", item.isCompleted ? "Tamamlandı" : "Bekliyor", item.isCompleted ? "badge done" : "badge"));
                tr.appendChild(statusCell);

                var actionCell = document.createElement("td");
                if (!item.isCompleted) {
                    var completeButton = make("button", "Tamamla", "link-btn");
                    completeButton.type = "button";
                    completeButton.addEventListener("click", function () { completeReminder(item.id); });
                    actionCell.appendChild(completeButton);
                }
                var deleteLink = make("button", "Sil", "link-btn");
                deleteLink.type = "button";
                deleteLink.addEventListener("click", function () { removeRecord("/api/Reminders/" + item.id, loadReminders); });
                actionCell.appendChild(deleteLink);
                tr.appendChild(actionCell);

                tbody.appendChild(tr);
            });
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });

        loadUpcoming();
    }

    function loadUpcoming() {
        var list = el("upcoming-list");
        api("/api/Reminders/upcoming?days=30").then(function (result) {
            var rows = (result && result.data) || [];
            clear(list);
            if (rows.length === 0) {
                list.appendChild(make("li", "Önümüzdeki 30 günde hatırlatma yok."));
                return;
            }
            rows.forEach(function (item) {
                var li = document.createElement("li");
                var left = document.createElement("div");
                left.appendChild(make("div", labelOf(REMINDER_TYPES, item.type) + " - " + item.plate));
                left.appendChild(make("div", formatDate(item.dueDate), "muted"));
                li.appendChild(left);

                var button = make("button", "Tamamla", "ghost");
                button.type = "button";
                button.addEventListener("click", function () { completeReminder(item.id); });
                li.appendChild(button);

                list.appendChild(li);
            });
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function completeReminder(id) {
        api("/api/Reminders/" + id + "/complete", { method: "PUT" }).then(function (result) {
            showMessage(el("app-message"), (result && result.message) || "Hatırlatma tamamlandı.", true);
            loadReminders();
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function removeRecord(path, reload) {
        api(path, { method: "DELETE" }).then(function (result) {
            showMessage(el("app-message"), (result && result.message) || "Kayıt silindi.", true);
            reload();
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function card(label, value, accent) {
        var box = make("div", null, accent ? "card accent" : "card");
        box.appendChild(make("span", label, "card-label"));
        box.appendChild(make("span", value, "card-value"));
        return box;
    }

    function loadSummary() {
        var start = el("report-start").value;
        var end = el("report-end").value;
        var cards = el("summary-cards");
        var tbody = el("category-rows");

        return api("/api/Reports/summary?vehicleId=" + state.selectedVehicleId + "&start=" + start + "&end=" + end).then(function (result) {
            var data = (result && result.data) || {};
            clear(cards);
            cards.appendChild(card("Yakıt", money(data.totalFuel)));
            cards.appendChild(card("Bakım", money(data.totalMaintenance)));
            cards.appendChild(card("Diğer masraf", money(data.totalOtherExpense)));
            cards.appendChild(card("Genel toplam", money(data.grandTotal), true));

            clear(tbody);
            var categories = data.categories || [];
            if (categories.length === 0) {
                emptyRow(tbody, 2, "Bu aralıkta masraf yok.");
                return;
            }
            categories.forEach(function (item) {
                var tr = document.createElement("tr");
                tr.appendChild(make("td", labelOf(EXPENSE_CATEGORIES, item.category)));
                tr.appendChild(make("td", money(item.total)));
                tbody.appendChild(tr);
            });
        });
    }

    function loadFuelStats() {
        var cards = el("fuel-cards");
        return api("/api/Reports/fuel-stats?vehicleId=" + state.selectedVehicleId).then(function (result) {
            var data = (result && result.data) || {};
            clear(cards);
            cards.appendChild(card("Ortalama tüketim", literFormat.format(Number(data.averageConsumptionPer100Km)) + " L/100km", true));
            cards.appendChild(card("Km başına maliyet", money(data.costPerKm), true));
            cards.appendChild(card("Toplam mesafe", km(data.totalKm)));
            cards.appendChild(card("Toplam yakıt", literFormat.format(Number(data.totalLiters)) + " L"));
            cards.appendChild(card("Toplam tutar", money(data.totalCost)));
        }).catch(function (error) {
            clear(cards);
            cards.appendChild(card("Yakıt istatistiği", error && error.message ? error.message : "Hesaplanamadı."));
        });
    }

    function loadMonthly() {
        return api("/api/Reports/monthly?vehicleId=" + state.selectedVehicleId).then(function (result) {
            var rows = (result && result.data) || [];
            drawChart(rows);
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function drawChart(rows) {
        var fallback = el("chart-fallback");
        var canvas = el("monthly-chart");

        if (typeof Chart === "undefined") {
            fallback.textContent = "Grafik kütüphanesi yüklenemedi (CDN erişimi yok). Aylık toplamlar tabloda görünmeye devam eder.";
            return;
        }

        fallback.textContent = rows.length === 0 ? "Grafik için henüz veri yok." : "";

        var labels = rows.map(function (item) {
            return String(item.month).padStart(2, "0") + "." + item.year;
        });
        var values = rows.map(function (item) { return Number(item.total); });

        if (state.chart) {
            state.chart.destroy();
        }

        state.chart = new Chart(canvas, {
            type: "bar",
            data: {
                labels: labels,
                datasets: [{
                    label: "Aylık toplam (TL)",
                    data: values,
                    backgroundColor: "rgba(255, 122, 26, 0.65)",
                    borderColor: "#ff7a1a",
                    borderWidth: 1,
                    borderRadius: 4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { labels: { color: "#99a1ad" } }
                },
                scales: {
                    x: { ticks: { color: "#99a1ad" }, grid: { color: "#262b33" } },
                    y: { ticks: { color: "#99a1ad" }, grid: { color: "#262b33" } }
                }
            }
        });
    }

    function bindAuth() {
        el("tab-login").addEventListener("click", function () { switchAuthTab(true); });
        el("tab-register").addEventListener("click", function () { switchAuthTab(false); });

        el("login-form").addEventListener("submit", function (event) {
            event.preventDefault();
            clearMessages();
            api("/api/Auth/login", {
                method: "POST",
                body: {
                    email: el("login-email").value,
                    password: el("login-password").value
                }
            }).then(function (result) {
                saveSession(result.data.token, result.data);
                el("login-password").value = "";
                enterApp();
            }).catch(function (error) {
                handleError(el("auth-message"), error);
            });
        });

        el("register-form").addEventListener("submit", function (event) {
            event.preventDefault();
            clearMessages();
            api("/api/Auth/register", {
                method: "POST",
                body: {
                    fullName: el("register-name").value,
                    email: el("register-email").value,
                    password: el("register-password").value,
                    companyName: el("register-company").value
                }
            }).then(function (result) {
                saveSession(result.data.token, result.data);
                el("register-password").value = "";
                enterApp();
            }).catch(function (error) {
                handleError(el("auth-message"), error);
            });
        });

        el("logout-btn").addEventListener("click", function () {
            goToLogin("Çıkış yapıldı.");
        });
    }

    function bindTeam() {
        el("team-btn").addEventListener("click", function () {
            var box = el("team-box");
            var acilacak = box.classList.contains("hidden");
            box.classList.toggle("hidden", !acilacak);
            el("team-credential").classList.add("hidden");
            if (acilacak) {
                loadTeam();
            }
        });

        el("team-close").addEventListener("click", function () {
            el("team-box").classList.add("hidden");
        });

        el("team-form").addEventListener("submit", function (event) {
            event.preventDefault();
            clearMessages();
            api("/api/Team", {
                method: "POST",
                body: {
                    fullName: el("team-name").value,
                    email: el("team-email").value,
                    role: el("team-role").value
                }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Üye eklendi.", true);
                el("team-form").reset();
                el("team-credential").classList.remove("hidden");
                el("team-credential-email").textContent = result.data.email;
                el("team-credential-password").textContent = result.data.temporaryPassword;
                loadTeam();
            }).catch(function (error) {
                handleError(el("app-message"), error);
            });
        });
    }

    function bindAssignment() {
        el("assignment-form").addEventListener("submit", function (event) {
            event.preventDefault();
            clearMessages();
            var userId = Number(el("assignment-user").value);
            if (!userId) {
                showMessage(el("app-message"), "Önce ekibe bir kullanıcı ekleyin.");
                return;
            }
            var devir = el("assignment-submit").textContent === "Devret";
            api(devir ? "/api/Assignments/transfer" : "/api/Assignments", {
                method: devir ? "PUT" : "POST",
                body: { vehicleId: state.selectedVehicleId, userId: userId }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Zimmet güncellendi.", true);
                loadAssignments();
            }).catch(function (error) {
                handleError(el("app-message"), error);
            });
        });

        el("assignment-end").addEventListener("click", function () {
            clearMessages();
            api("/api/Assignments/end", {
                method: "PUT",
                body: { vehicleId: state.selectedVehicleId }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Zimmet sonlandırıldı.", true);
                loadAssignments();
            }).catch(function (error) {
                handleError(el("app-message"), error);
            });
        });
    }

    var RECEIPT_TYPES = [
        ["Yakit", "Yakıt"],
        ["Bakim", "Bakım"],
        ["Masraf", "Masraf"]
    ];

    function receiptTypeChanged() {
        var tur = el("receipt-type").value;
        el("receipt-liters-box").classList.toggle("hidden", tur !== "Yakit");
        el("receipt-maintenance-box").classList.toggle("hidden", tur !== "Bakim");
        el("receipt-category-box").classList.toggle("hidden", tur !== "Masraf");
    }

    function markNull(node, isNull) {
        node.classList.toggle("needs-value", isNull);
    }

    function fillReceiptVehicles(selectedId) {
        var select = el("receipt-vehicle");
        clear(select);
        state.vehicles.forEach(function (vehicle) {
            var option = document.createElement("option");
            option.value = String(vehicle.id);
            option.textContent = vehicle.plate + " - " + vehicle.brand + " " + vehicle.model;
            select.appendChild(option);
        });
        if (selectedId) {
            select.value = String(selectedId);
        } else if (state.selectedVehicleId) {
            select.value = String(state.selectedVehicleId);
        }
    }

    function showReceiptReview(draft) {
        state.receiptDraft = draft;
        el("receipt-review").classList.remove("hidden");

        var guven = Math.round((Number(draft.guvenSkoru) || 0) * 100);
        el("receipt-confidence").textContent = guven > 0
            ? "Okuma güveni %" + guven + ". Boş kalan alanları siz doldurun."
            : "Fiş okunamadı. Alanları elle doldurabilirsiniz.";

        fillReceiptVehicles(draft.vehicleId);

        el("receipt-type").value = draft.tahminiTur === "Bilinmiyor" ? "Masraf" : draft.tahminiTur;
        receiptTypeChanged();

        var tarih = el("receipt-date");
        tarih.value = draft.tarih ? String(draft.tarih).slice(0, 10) : "";
        markNull(tarih, !draft.tarih);

        var tutar = el("receipt-amount");
        tutar.value = draft.toplamTutar === null || draft.toplamTutar === undefined ? "" : draft.toplamTutar;
        markNull(tutar, draft.toplamTutar === null || draft.toplamTutar === undefined);

        var kilometre = el("receipt-km");
        kilometre.value = draft.km === null || draft.km === undefined ? "" : draft.km;
        markNull(kilometre, draft.km === null || draft.km === undefined);

        var litre = el("receipt-liters");
        litre.value = draft.litre === null || draft.litre === undefined ? "" : draft.litre;
        markNull(litre, draft.litre === null || draft.litre === undefined);

        el("receipt-note").value = "";
        renderReceiptParts(draft.parcalar);
    }

    function hideReceiptReview() {
        state.receiptDraft = null;
        el("receipt-review").classList.add("hidden");
    }

    function loadPendingReceipts() {
        return api("/api/Receipts?durum=Bekliyor").then(function (result) {
            var rows = (result && result.data) || [];
            var liste = el("receipt-pending");
            clear(liste);

            var rozet = el("receipt-badge");
            rozet.textContent = rows.length ? String(rows.length) : "";
            rozet.classList.toggle("hidden", rows.length === 0);
            el("receipt-pending-title").classList.toggle("hidden", rows.length === 0);

            rows.forEach(function (item) {
                var li = document.createElement("li");
                var ozet = item.orijinalAd;
                if (item.toplamTutar !== null && item.toplamTutar !== undefined) {
                    ozet += " · " + money(item.toplamTutar);
                }
                li.appendChild(make("span", ozet));

                var actions = make("span", "", "row-actions");
                var ac = make("button", "İncele", "link-btn");
                ac.type = "button";
                ac.addEventListener("click", function () {
                    el("receipt-box").classList.remove("hidden");
                    showReceiptReview(item);
                });
                actions.appendChild(ac);
                li.appendChild(actions);
                liste.appendChild(li);
            });
        }).catch(function () {
            el("receipt-badge").classList.add("hidden");
        });
    }

    var PART_TYPES = [
        ["MotorYagi", "Motor yağı"],
        ["YagFiltresi", "Yağ filtresi"],
        ["HavaFiltresi", "Hava filtresi"],
        ["PolenFiltresi", "Polen filtresi"],
        ["YakitFiltresi", "Yakıt filtresi"],
        ["FrenBalatasiOn", "Ön fren balatası"],
        ["FrenBalatasiArka", "Arka fren balatası"],
        ["FrenDiskiOn", "Ön fren diski"],
        ["FrenDiskiArka", "Arka fren diski"],
        ["Buji", "Buji"],
        ["TrigerSeti", "Triger seti"],
        ["VKayisi", "V kayışı"],
        ["Aku", "Akü"],
        ["Lastik", "Lastik"],
        ["Amortisor", "Amortisör"],
        ["Silecek", "Silecek"],
        ["Antifriz", "Antifriz"],
        ["FrenHidroligi", "Fren hidroliği"],
        ["SanzimanYagi", "Şanzıman yağı"],
        ["Devirdaim", "Devirdaim"],
        ["RotBasi", "Rot başı"],
        ["Salincak", "Salıncak"],
        ["Debriyaj", "Debriyaj"],
        ["Diger", "Diğer"]
    ];

    var PART_STATUS = {
        Iyi: "İyi",
        Yaklasiyor: "Yaklaşıyor",
        Gecti: "Geçti"
    };

    function addPartRow(kutu, deger) {
        var satir = make("div", "", "part-row");

        var tur = document.createElement("select");
        fillSelect(tur, PART_TYPES);
        tur.className = "part-type";
        if (deger && deger.parcaTuru) {
            tur.value = deger.parcaTuru;
        }
        satir.appendChild(tur);

        var aciklama = document.createElement("input");
        aciklama.type = "text";
        aciklama.className = "part-desc";
        aciklama.placeholder = "Açıklama";
        aciklama.value = deger && deger.aciklama ? deger.aciklama : "";
        satir.appendChild(aciklama);

        var adet = document.createElement("input");
        adet.type = "number";
        adet.min = "1";
        adet.className = "part-qty";
        adet.value = deger && deger.adet ? deger.adet : 1;
        satir.appendChild(adet);

        var tutar = document.createElement("input");
        tutar.type = "number";
        tutar.min = "0";
        tutar.step = "0.01";
        tutar.className = "part-cost";
        tutar.placeholder = "Tutar";
        tutar.value = deger && deger.tutar !== null && deger.tutar !== undefined ? deger.tutar : "";
        satir.appendChild(tutar);

        var sil = make("button", "Sil", "link-btn");
        sil.type = "button";
        sil.addEventListener("click", function () { kutu.removeChild(satir); });
        satir.appendChild(sil);

        kutu.appendChild(satir);
    }

    function readPartRows(kutu) {
        var parcalar = [];
        Array.prototype.forEach.call(kutu.querySelectorAll(".part-row"), function (satir) {
            var tutar = satir.querySelector(".part-cost").value;
            parcalar.push({
                parcaTuru: satir.querySelector(".part-type").value,
                aciklama: satir.querySelector(".part-desc").value,
                adet: Number(satir.querySelector(".part-qty").value) || 1,
                tutar: tutar ? Number(tutar) : null,
                marka: null
            });
        });
        return parcalar;
    }

    function renderReceiptParts(parcalar) {
        var kutu = el("receipt-parts-box");
        var liste = el("receipt-parts");
        clear(liste);

        var varMi = parcalar && parcalar.length > 0;
        kutu.classList.toggle("hidden", !varMi);

        if (!varMi) {
            return;
        }

        parcalar.forEach(function (parca) {
            var li = document.createElement("li");
            var metin = labelOf(PART_TYPES, parca.parcaTuru) + " — " + (parca.aciklama || "");
            if (parca.tutar !== null && parca.tutar !== undefined) {
                metin += " · " + money(parca.tutar);
            }
            li.appendChild(make("span", metin));
            liste.appendChild(li);
        });
    }

    function loadPartMemory() {
        if (!state.selectedVehicleId) {
            return;
        }
        var tbody = el("parca-rows");
        api("/api/Vehicles/" + state.selectedVehicleId + "/parca-hafizasi").then(function (result) {
            var rows = (result && result.data) || [];
            clear(tbody);

            if (rows.length === 0) {
                emptyRow(tbody, 7, "Bakım kayıtlarında parça yok.");
                return;
            }

            rows.forEach(function (item) {
                var tr = document.createElement("tr");
                tr.appendChild(make("td", item.parcaAdi));
                tr.appendChild(make("td", formatDate(item.sonDegisimTarihi) + (item.sonDegisimKm ? " · " + km(item.sonDegisimKm) : "")));
                tr.appendChild(make("td", item.degisimSayisi));
                tr.appendChild(make("td", money(item.toplamTutar)));

                var sonraki = [];
                if (item.sonrakiTahminiKm) {
                    sonraki.push(km(item.sonrakiTahminiKm));
                }
                if (item.sonrakiTahminiTarih) {
                    sonraki.push(formatDate(item.sonrakiTahminiTarih));
                }
                tr.appendChild(make("td", sonraki.length ? sonraki.join(" / ") : "-"));

                tr.appendChild(make("td", PART_STATUS[item.durum] || item.durum, "durum-" + item.durum.toLowerCase()));

                var hucre = document.createElement("td");
                if (item.sonrakiTahminiKm || item.sonrakiTahminiTarih) {
                    var dugme = make("button", "Hatırlatma oluştur", "link-btn");
                    dugme.type = "button";
                    dugme.addEventListener("click", function () { createPartReminder(item.parcaTuru); });
                    hucre.appendChild(dugme);
                }
                tr.appendChild(hucre);

                tbody.appendChild(tr);
            });
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function createPartReminder(parcaTuru) {
        clearMessages();
        api("/api/Vehicles/" + state.selectedVehicleId + "/parca-hafizasi/" + parcaTuru + "/hatirlatma", { method: "POST" })
            .then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Hatırlatma eklendi.", true);
            })
            .catch(function (error) {
                handleError(el("app-message"), error);
            });
    }

    function bindParts() {
        el("maintenance-part-add").addEventListener("click", function () {
            addPartRow(el("maintenance-parts"), null);
        });
    }

    function karneKapsamiOku() {
        return {
            bakimGecmisi: el("karne-bakim").checked,
            parcaHafizasi: el("karne-parca").checked,
            yakitOzeti: el("karne-yakit").checked,
            belgeler: el("karne-belge").checked,
            plakaGoster: el("karne-plaka").checked,
            tutarGoster: el("karne-tutar").checked
        };
    }

    function karneSonucGoster(veri) {
        el("karne-sonuc").classList.remove("hidden");
        el("karne-url").textContent = veri.url;
        el("karne-goruntulenme").textContent = "Görüntülenme: " + (veri.goruntulenmeSayisi || 0);

        try {
            window.GarajimQR.canvasaCiz(el("karne-qr"), veri.url, 4, 2);
        } catch (hata) {
            el("karne-qr").classList.add("hidden");
        }
    }

    function bindKarne() {
        el("karne-btn").addEventListener("click", function () {
            var kutu = el("karne-box");
            var acilacak = kutu.classList.contains("hidden");
            kutu.classList.toggle("hidden", !acilacak);
            if (acilacak) {
                el("karne-sonuc").classList.add("hidden");
            }
        });

        el("karne-close").addEventListener("click", function () {
            el("karne-box").classList.add("hidden");
        });

        el("karne-form").addEventListener("submit", function (event) {
            event.preventDefault();
            clearMessages();

            if (!state.selectedVehicleId) {
                showMessage(el("app-message"), "Önce bir araç seçin.");
                return;
            }

            var sure = el("karne-sure").value;

            api("/api/Vehicles/" + state.selectedVehicleId + "/karne", {
                method: "POST",
                body: {
                    kapsam: karneKapsamiOku(),
                    sonKullanmaGun: sure ? Number(sure) : null
                }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Bağlantı oluşturuldu.", true);
                karneSonucGoster(result.data);
            }).catch(function (error) {
                handleError(el("app-message"), error);
            });
        });

        el("karne-kapat").addEventListener("click", function () {
            clearMessages();
            if (!state.selectedVehicleId) {
                return;
            }

            api("/api/Vehicles/" + state.selectedVehicleId + "/karne", { method: "DELETE" })
                .then(function (result) {
                    showMessage(el("app-message"), (result && result.message) || "Paylaşım kapatıldı.", true);
                    el("karne-sonuc").classList.add("hidden");
                })
                .catch(function (error) {
                    handleError(el("app-message"), error);
                });
        });

        el("karne-kopyala").addEventListener("click", function () {
            var metin = el("karne-url").textContent;
            if (!metin) {
                return;
            }

            if (navigator.clipboard && navigator.clipboard.writeText) {
                navigator.clipboard.writeText(metin).then(function () {
                    showMessage(el("app-message"), "Bağlantı kopyalandı.", true);
                });
            }
        });
    }

    function bulkUploadOne(dosya, otoOnay) {
        var form = new FormData();
        form.append("file", dosya);
        return api("/api/Receipts?otoOnay=" + (otoOnay ? "true" : "false"), { method: "POST", body: form })
            .then(function (result) {
                return { ok: true, ad: dosya.name, veri: result.data };
            })
            .catch(function (error) {
                return { ok: false, ad: dosya.name, hata: error.message || "Yüklenemedi." };
            });
    }

    function renderBulkSummary(sonuclar) {
        var onaylandi = sonuclar.filter(function (s) { return s.ok && s.veri.durum === "Onaylandi"; });
        var bekliyor = sonuclar.filter(function (s) { return s.ok && s.veri.durum === "Bekliyor"; });
        var hatali = sonuclar.filter(function (s) { return !s.ok; });

        el("bulk-summary").classList.remove("hidden");
        el("bulk-summary-line").textContent =
            "Onaylandı " + onaylandi.length + " · Bekliyor " + bekliyor.length + " · Hata " + hatali.length;

        var liste = el("bulk-summary-list");
        clear(liste);

        onaylandi.forEach(function (s) {
            var li = document.createElement("li");
            li.appendChild(make("span", s.ad + " — kaydedildi"));
            var rozet = make("span", "oto", "badge-oto");
            li.appendChild(rozet);
            liste.appendChild(li);
        });

        bekliyor.forEach(function (s) {
            var li = document.createElement("li");
            li.appendChild(make("span", s.ad + " — " + (s.veri.atlamaNedeni || "Kontrol bekliyor")));
            liste.appendChild(li);
        });

        hatali.forEach(function (s) {
            var li = document.createElement("li");
            li.appendChild(make("span", s.ad + " — " + s.hata));
            liste.appendChild(li);
        });
    }

    function bindBulkUpload() {
        el("bulk-form").addEventListener("submit", function (event) {
            event.preventDefault();
            clearMessages();

            var input = el("bulk-files");
            if (!input.files || input.files.length === 0) {
                showMessage(el("app-message"), "Önce fiş dosyalarını seçin.");
                return;
            }

            var dosyalar = Array.prototype.slice.call(input.files);
            var otoOnay = el("bulk-auto").checked;
            var sonuclar = [];
            var ilerleme = el("bulk-progress");

            el("bulk-summary").classList.add("hidden");
            ilerleme.classList.remove("hidden");
            ilerleme.textContent = "0/" + dosyalar.length;

            var zincir = Promise.resolve();
            dosyalar.forEach(function (dosya, sira) {
                zincir = zincir.then(function () {
                    return bulkUploadOne(dosya, otoOnay).then(function (sonuc) {
                        sonuclar.push(sonuc);
                        ilerleme.textContent = (sira + 1) + "/" + dosyalar.length;
                    });
                });
            });

            zincir.then(function () {
                ilerleme.classList.add("hidden");
                el("bulk-form").reset();
                el("bulk-auto").checked = otoOnay;
                renderBulkSummary(sonuclar);
                loadPendingReceipts();
                loadVehicles();
            });
        });
    }

    function bindReceipts() {
        el("receipt-btn").addEventListener("click", function () {
            var box = el("receipt-box");
            var acilacak = box.classList.contains("hidden");
            box.classList.toggle("hidden", !acilacak);
            if (acilacak) {
                hideReceiptReview();
                el("receipt-form").reset();
                loadPendingReceipts();
            }
        });

        el("receipt-close").addEventListener("click", function () {
            el("receipt-box").classList.add("hidden");
            hideReceiptReview();
        });

        el("receipt-type").addEventListener("change", receiptTypeChanged);

        el("receipt-form").addEventListener("submit", function (event) {
            event.preventDefault();
            clearMessages();

            var input = el("receipt-file");
            if (!input.files || input.files.length === 0) {
                showMessage(el("app-message"), "Önce bir fiş fotoğrafı seçin.");
                return;
            }

            var form = new FormData();
            form.append("file", input.files[0]);

            el("receipt-progress").classList.remove("hidden");

            api("/api/Receipts?otoOnay=false", { method: "POST", body: form }).then(function (result) {
                el("receipt-progress").classList.add("hidden");
                showMessage(el("app-message"), (result && result.message) || "Fiş okundu.", true);
                el("receipt-form").reset();
                showReceiptReview(result.data.taslak);
                loadPendingReceipts();
            }).catch(function (error) {
                el("receipt-progress").classList.add("hidden");
                handleError(el("app-message"), error);
            });
        });

        el("receipt-confirm-form").addEventListener("submit", function (event) {
            event.preventDefault();
            clearMessages();

            if (!state.receiptDraft) {
                return;
            }

            var tur = el("receipt-type").value;
            var kilometre = el("receipt-km").value;
            var litre = el("receipt-liters").value;

            api("/api/Receipts/" + state.receiptDraft.id + "/confirm", {
                method: "POST",
                body: {
                    vehicleId: Number(el("receipt-vehicle").value),
                    tur: tur,
                    tarih: el("receipt-date").value,
                    tutar: Number(el("receipt-amount").value),
                    km: kilometre ? Number(kilometre) : null,
                    litre: tur === "Yakit" && litre ? Number(litre) : null,
                    bakimTuru: tur === "Bakim" ? el("receipt-maintenance-type").value : null,
                    parcalar: tur === "Bakim" && state.receiptDraft.parcalar ? state.receiptDraft.parcalar : null,
                    masrafKategorisi: tur === "Masraf" ? el("receipt-category").value : null,
                    not: el("receipt-note").value
                }
            }).then(function (result) {
                hideReceiptReview();
                loadPendingReceipts();
                loadVehicles();
                selectTab(tur === "Yakit" ? "yakit" : tur === "Bakim" ? "bakim" : "masraf");
                showMessage(el("app-message"), (result && result.message) || "Kayıt oluşturuldu.", true);
            }).catch(function (error) {
                handleError(el("app-message"), error);
            });
        });

        el("receipt-reject").addEventListener("click", function () {
            if (!state.receiptDraft) {
                return;
            }
            clearMessages();
            api("/api/Receipts/" + state.receiptDraft.id + "/reject", { method: "POST" }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Taslak silindi.", true);
                hideReceiptReview();
                loadPendingReceipts();
            }).catch(function (error) {
                handleError(el("app-message"), error);
            });
        });
    }

    function bindDocuments() {
        el("document-close").addEventListener("click", closeDocuments);

        el("document-form").addEventListener("submit", function (event) {
            event.preventDefault();
            clearMessages();
            if (!state.documentRecordId) {
                showMessage(el("app-message"), "Önce bir bakım kaydının Belgeler düğmesine basın.");
                return;
            }
            var input = el("document-file");
            if (!input.files || input.files.length === 0) {
                showMessage(el("app-message"), "Önce bir dosya seçin.");
                return;
            }
            var form = new FormData();
            form.append("file", input.files[0]);
            form.append("maintenanceRecordId", String(state.documentRecordId));

            api("/api/Documents", { method: "POST", body: form }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Belge yüklendi.", true);
                el("document-form").reset();
                loadDocuments();
            }).catch(function (error) {
                handleError(el("app-message"), error);
            });
        });
    }

    function bindVehicle() {
        el("vehicle-select").addEventListener("change", function (event) {
            state.selectedVehicleId = Number(event.target.value);
            clearMessages();
            closeDocuments();
            loadActiveTab();
        });

        el("add-vehicle-btn").addEventListener("click", function () {
            el("vehicle-form-box").classList.remove("hidden");
            el("vehicle-year").value = new Date().getFullYear();
        });

        el("vehicle-cancel").addEventListener("click", function () {
            el("vehicle-form-box").classList.add("hidden");
        });

        el("vehicle-form").addEventListener("submit", function (event) {
            event.preventDefault();
            clearMessages();
            api("/api/Vehicles", {
                method: "POST",
                body: {
                    plate: el("vehicle-plate").value,
                    brand: el("vehicle-brand").value,
                    model: el("vehicle-model").value,
                    year: Number(el("vehicle-year").value),
                    currentKm: Number(el("vehicle-km").value),
                    fuelType: el("vehicle-fuel").value
                }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Araç eklendi.", true);
                el("vehicle-form").reset();
                el("vehicle-form-box").classList.add("hidden");
                state.selectedVehicleId = result.data.id;
                loadVehicles();
            }).catch(function (error) {
                handleError(el("app-message"), error);
            });
        });
    }

    function selectTab(tab) {
        var buttons = document.querySelectorAll(".tab-btn");
        Array.prototype.forEach.call(buttons, function (button) {
            button.classList.toggle("active", button.getAttribute("data-tab") === tab);
        });
        var panels = document.querySelectorAll(".tab-panel");
        Array.prototype.forEach.call(panels, function (panel) {
            panel.classList.add("hidden");
        });
        el("panel-" + tab).classList.remove("hidden");
        clearMessages();
        loadActiveTab();
    }

    function bindTabs() {
        var buttons = document.querySelectorAll(".tab-btn");
        Array.prototype.forEach.call(buttons, function (button) {
            button.addEventListener("click", function () {
                selectTab(button.getAttribute("data-tab"));
            });
        });
    }

    function bindRecordForms() {
        el("maintenance-form").addEventListener("submit", function (event) {
            event.preventDefault();
            clearMessages();
            api("/api/Maintenance", {
                method: "POST",
                body: {
                    vehicleId: state.selectedVehicleId,
                    parcalar: readPartRows(el("maintenance-parts")),
                    type: el("maintenance-type").value,
                    date: el("maintenance-date").value,
                    km: Number(el("maintenance-km").value),
                    cost: Number(el("maintenance-cost").value),
                    serviceName: el("maintenance-service").value,
                    note: el("maintenance-note").value
                }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Kayıt eklendi.", true);
                el("maintenance-form").reset();
                clear(el("maintenance-parts"));
                el("maintenance-date").value = todayInput();
                loadMaintenance();
                loadVehicles();
            }).catch(function (error) {
                handleError(el("app-message"), error);
            });
        });

        el("fuel-form").addEventListener("submit", function (event) {
            event.preventDefault();
            clearMessages();
            api("/api/Fuel", {
                method: "POST",
                body: {
                    vehicleId: state.selectedVehicleId,
                    date: el("fuel-date").value,
                    km: Number(el("fuel-km").value),
                    liters: Number(el("fuel-liters").value),
                    totalCost: Number(el("fuel-cost").value)
                }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Kayıt eklendi.", true);
                el("fuel-form").reset();
                el("fuel-date").value = todayInput();
                loadFuel();
                loadVehicles();
            }).catch(function (error) {
                handleError(el("app-message"), error);
            });
        });

        el("expense-form").addEventListener("submit", function (event) {
            event.preventDefault();
            clearMessages();
            api("/api/Expenses", {
                method: "POST",
                body: {
                    vehicleId: state.selectedVehicleId,
                    category: el("expense-category").value,
                    date: el("expense-date").value,
                    amount: Number(el("expense-amount").value),
                    note: el("expense-note").value
                }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Kayıt eklendi.", true);
                el("expense-form").reset();
                el("expense-date").value = todayInput();
                loadExpenses();
            }).catch(function (error) {
                handleError(el("app-message"), error);
            });
        });

        el("reminder-form").addEventListener("submit", function (event) {
            event.preventDefault();
            clearMessages();
            var dateValue = el("reminder-date").value;
            var kmValue = el("reminder-km").value;
            api("/api/Reminders", {
                method: "POST",
                body: {
                    vehicleId: state.selectedVehicleId,
                    type: el("reminder-type").value,
                    dueDate: dateValue ? dateValue : null,
                    dueKm: kmValue ? Number(kmValue) : null,
                    note: el("reminder-note").value
                }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Hatırlatma eklendi.", true);
                el("reminder-form").reset();
                loadReminders();
            }).catch(function (error) {
                handleError(el("app-message"), error);
            });
        });

        el("report-form").addEventListener("submit", function (event) {
            event.preventDefault();
            clearMessages();
            loadSummary().catch(function (error) {
                handleError(el("app-message"), error);
            });
            loadMonthly();
            loadFuelStats();
        });

        el("price-form").addEventListener("submit", function (event) {
            event.preventDefault();
            clearMessages();
            api("/api/price/estimate", {
                method: "POST",
                body: {
                    marka: el("price-marka").value,
                    seri: el("price-seri").value,
                    yil: Number(el("price-yil").value),
                    kilometre: Number(el("price-km").value),
                    yakitTipi: el("price-yakit").value,
                    vitesTipi: el("price-vites").value,
                    kasaTipi: el("price-kasa").value
                }
            }).then(function (result) {
                var data = result.data;
                el("price-result").classList.remove("hidden");
                el("price-value").textContent = wholeFormat.format(Number(data.tahminiFiyat)) + " " + data.paraBirimi;
                el("price-detail").textContent = data.yil + " " + data.marka + " " + data.seri + " - " + km(data.kilometre);
            }).catch(function (error) {
                el("price-result").classList.add("hidden");
                handleError(el("app-message"), error);
            });
        });
    }

    function initSelects() {
        fillSelect(el("team-role"), TEAM_ROLES);
        fillSelect(el("receipt-type"), RECEIPT_TYPES);
        fillSelect(el("receipt-maintenance-type"), MAINTENANCE_TYPES);
        fillSelect(el("receipt-category"), EXPENSE_CATEGORIES);
        fillSelect(el("vehicle-fuel"), FUEL_TYPES);
        fillSelect(el("maintenance-type"), MAINTENANCE_TYPES);
        fillSelect(el("expense-category"), EXPENSE_CATEGORIES);
        fillSelect(el("reminder-type"), REMINDER_TYPES);
        fillSimpleSelect(el("price-yakit"), PRICE_FUEL);
        fillSimpleSelect(el("price-vites"), PRICE_GEAR);
        fillSimpleSelect(el("price-kasa"), PRICE_BODY);
    }

    function initDates() {
        var today = todayInput();
        el("maintenance-date").value = today;
        el("fuel-date").value = today;
        el("expense-date").value = today;
        el("report-end").value = today;
        var start = new Date();
        start.setMonth(start.getMonth() - 6);
        el("report-start").value = start.toISOString().slice(0, 10);
    }

    function init() {
        initSelects();
        initDates();
        bindAuth();
        bindVehicle();
        bindTabs();
        bindRecordForms();
        bindTeam();
        bindAssignment();
        bindDocuments();
        bindReceipts();
        bindBulkUpload();
        bindParts();
        bindKarne();

        if (readSession()) {
            enterApp();
        }
    }

    function registerServiceWorker() {
        if (!("serviceWorker" in navigator)) {
            return;
        }
        window.addEventListener("load", function () {
            navigator.serviceWorker.register("/sw.js").catch(function () {
            });
        });
    }

    registerServiceWorker();
    document.addEventListener("DOMContentLoaded", init);
})();

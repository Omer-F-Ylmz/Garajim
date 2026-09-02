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
        chart: null,
        maliyetChart: null,
        tuketimChart: null
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

    function grafikleriTemizle() {
        ["chart", "maliyetChart", "tuketimChart"].forEach(function (ad) {
            if (state[ad]) {
                state[ad].destroy();
                state[ad] = null;
            }
        });
    }

    function goToLogin(message) {
        clearSession();
        grafikleriTemizle();
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
        el("team-btn").classList.toggle("hidden", !canManage());
        el("team-form").classList.toggle("hidden", !isOwner());
        el("plan-form").classList.toggle("hidden", !isOwner());
        el("karne-btn").classList.toggle("hidden", !canManage());

        var zimmetTab = document.querySelector('.tab-btn[data-manager-only="true"]');
        if (zimmetTab) {
            zimmetTab.classList.toggle("hidden", !canManage());
            if (!canManage() && zimmetTab.classList.contains("active")) {
                selectTab("bakim");
            }
        }
        if (!canManage()) {
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
        loadPanelUyarisi();
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
            yakitAlanlariniAyarla();
            loadFuel();
        } else if (tab === "masraf") {
            loadExpenses();
        } else if (tab === "hatirlatma") {
            loadReminders();
        } else if (tab === "rapor") {
            loadFuelStats();
            loadMonthly();
            loadMaliyet();
            loadFiloMaliyet();
        } else if (tab === "zimmet") {
            loadAssignments();
        } else if (tab === "yolculuk") {
            loadYolculuk();
        } else if (tab === "lastik") {
            loadLastik();
        } else if (tab === "parca") {
            loadPartMemory();
        } else if (tab === "evrak") {
            loadEvrak();
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
        loadBelgeler();
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
                if (!isOwner() || (state.user && item.email === state.user.email)) {
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
                if (isOwner() && item.isActive && !kendisi) {
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
                emptyRow(tbody, 6, "Kayıt yok.");
                return;
            }
            rows.forEach(function (item) {
                var tr = document.createElement("tr");
                tr.appendChild(make("td", formatDate(item.date)));
                tr.appendChild(make("td", km(item.km)));
                tr.appendChild(make("td", Number(item.liters) > 0 ? literFormat.format(Number(item.liters)) + " L" : "-"));
                tr.appendChild(make("td", item.kwh === null ? "-" : literFormat.format(Number(item.kwh)) + " kWh" + (item.sarjTuru ? " (" + labelOf(SARJ_TURU, item.sarjTuru) + ")" : "")));
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

    function raporAraligi() {
        return "baslangic=" + el("report-start").value + "&bitis=" + el("report-end").value;
    }

    function loadMaliyet() {
        var cards = el("maliyet-cards");
        return api("/api/Vehicles/" + state.selectedVehicleId + "/maliyet?" + raporAraligi()).then(function (result) {
            var data = (result && result.data) || {};

            clear(cards);
            cards.appendChild(card("Toplam maliyet", money(data.toplamMaliyet), true));
            cards.appendChild(card("Km başına maliyet", data.maliyetKmBasi === null ? "—" : money(data.maliyetKmBasi), true));
            cards.appendChild(card("Ortalama tüketim", data.litre100Km === null ? "—" : literFormat.format(Number(data.litre100Km)) + " L/100km"));
            if (data.kwh100Km !== null || Number(data.toplamKwh) > 0) {
                cards.appendChild(card("Şarj tüketimi", data.kwh100Km === null ? "—" : literFormat.format(Number(data.kwh100Km)) + " kWh/100km"));
                cards.appendChild(card("Toplam şarj", literFormat.format(Number(data.toplamKwh)) + " kWh"));
            }
            cards.appendChild(card("Mesafe", km(data.mesafeKm)));
            cards.appendChild(card("Yakıt", money(data.toplamYakit)));
            cards.appendChild(card("Bakım", money(data.toplamBakim)));
            cards.appendChild(card("Masraf", money(data.toplamMasraf)));

            drawMaliyetChart(data.aylikSeri || []);
            drawTuketimChart(data.tuketimSeri || []);
        }).catch(function (error) {
            clear(cards);
            cards.appendChild(card("Maliyet", error && error.message ? error.message : "Hesaplanamadı."));
        });
    }

    function loadFiloMaliyet() {
        var tbody = el("filo-rows");
        return api("/api/Reports/filo-maliyet?" + raporAraligi()).then(function (result) {
            var araclar = (result && result.data && result.data.araclar) || [];
            clear(tbody);

            if (araclar.length === 0) {
                emptyRow(tbody, 5, "Bu aralıkta filo verisi yok.");
                return;
            }

            araclar.forEach(function (satir) {
                var tr = document.createElement("tr");
                tr.appendChild(make("td", satir.plaka + " - " + satir.marka + " " + satir.model));
                tr.appendChild(make("td", money(satir.toplamMaliyet)));
                tr.appendChild(make("td", km(satir.mesafeKm)));
                tr.appendChild(make("td", satir.maliyetKmBasi === null ? "—" : money(satir.maliyetKmBasi)));
                tr.appendChild(make("td", satir.litre100Km === null ? "—" : literFormat.format(Number(satir.litre100Km))));
                tbody.appendChild(tr);
            });
        }).catch(function (error) {
            clear(tbody);
            emptyRow(tbody, 5, error && error.message ? error.message : "Filo karşılaştırması alınamadı.");
        });
    }

    function ayEtiketi(kalem) {
        return String(kalem.ay).padStart(2, "0") + "." + kalem.yil;
    }

    function grafikSecenekleri(yiginli) {
        return {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { labels: { color: "#99a1ad" } } },
            scales: {
                x: { stacked: yiginli, ticks: { color: "#99a1ad" }, grid: { color: "#262b33" } },
                y: { stacked: yiginli, ticks: { color: "#99a1ad" }, grid: { color: "#262b33" } }
            }
        };
    }

    function drawMaliyetChart(seri) {
        var fallback = el("maliyet-fallback");

        if (typeof Chart === "undefined") {
            fallback.textContent = "Grafik kütüphanesi yüklenemedi (CDN erişimi yok). Maliyet kartları görünmeye devam eder.";
            return;
        }

        var dolu = seri.some(function (kalem) { return Number(kalem.toplam) > 0; });
        fallback.textContent = dolu ? "" : "Grafik için henüz veri yok.";

        if (state.maliyetChart) {
            state.maliyetChart.destroy();
        }

        state.maliyetChart = new Chart(el("maliyet-chart"), {
            type: "bar",
            data: {
                labels: seri.map(ayEtiketi),
                datasets: [
                    { label: "Yakıt", data: seri.map(function (k) { return Number(k.yakit); }), backgroundColor: "rgba(255, 122, 26, 0.75)" },
                    { label: "Bakım", data: seri.map(function (k) { return Number(k.bakim); }), backgroundColor: "rgba(90, 160, 255, 0.75)" },
                    { label: "Masraf", data: seri.map(function (k) { return Number(k.masraf); }), backgroundColor: "rgba(140, 200, 140, 0.75)" }
                ]
            },
            options: grafikSecenekleri(true)
        });
    }

    function drawTuketimChart(seri) {
        var fallback = el("tuketim-fallback");

        if (typeof Chart === "undefined") {
            fallback.textContent = "";
            return;
        }

        fallback.textContent = seri.length === 0 ? "Tüketim grafiği için en az iki yakıt kaydı gerekir." : "";

        if (state.tuketimChart) {
            state.tuketimChart.destroy();
        }

        state.tuketimChart = new Chart(el("tuketim-chart"), {
            type: "line",
            data: {
                labels: seri.map(ayEtiketi),
                datasets: [{
                    label: "Tüketim (L/100km)",
                    data: seri.map(function (k) { return Number(k.litre100Km); }),
                    borderColor: "#ff7a1a",
                    backgroundColor: "rgba(255, 122, 26, 0.25)",
                    tension: 0.25,
                    fill: true
                }]
            },
            options: grafikSecenekleri(false)
        });
    }

    var YOLCULUK_AMAC = [
        ["Is", "İş"],
        ["Ozel", "Özel"]
    ];

    var BELGE_DURUM = {
        Gecti: "Süresi geçti",
        Yaklasiyor: "Yaklaşıyor",
        Iyi: "İyi"
    };

    function yolculukAraligi() {
        var bas = el("report-start").value;
        var son = el("report-end").value;
        var parcalar = [];
        if (bas) {
            parcalar.push("baslangic=" + bas);
        }
        if (son) {
            parcalar.push("bitis=" + son);
        }
        return parcalar.join("&");
    }

    function renderYolculukRows(kayitlar) {
        var tbody = el("yolculuk-rows");
        clear(tbody);

        if (kayitlar.length === 0) {
            emptyRow(tbody, 8, "Bu araç için yolculuk kaydı yok.");
            return;
        }

        kayitlar.forEach(function (kayit) {
            var tr = document.createElement("tr");
            tr.appendChild(make("td", formatDate(kayit.tarih)));
            tr.appendChild(make("td", labelOf(YOLCULUK_AMAC, kayit.amac)));
            tr.appendChild(make("td", km(kayit.baslangicKm)));
            tr.appendChild(make("td", km(kayit.bitisKm)));
            tr.appendChild(make("td", km(kayit.mesafeKm)));

            var guzergah = [kayit.nereden, kayit.nereye].filter(Boolean).join(" → ");
            tr.appendChild(make("td", guzergah || "-"));
            tr.appendChild(make("td", kayit.surucuAdi || "-"));

            tr.appendChild(deleteButton(function () { yolculukSil(kayit.id); }));

            tbody.appendChild(tr);
        });
    }

    function loadYolculuk() {
        if (!state.selectedVehicleId) {
            clear(el("yolculuk-rows"));
            clear(el("yolculuk-cards"));
            return Promise.resolve();
        }

        var sorgu = "vehicleId=" + state.selectedVehicleId;
        var aralik = yolculukAraligi();
        if (aralik) {
            sorgu += "&" + aralik;
        }

        return api("/api/Yolculuk?" + sorgu).then(function (result) {
            renderYolculukRows((result && result.data) || []);
            return api("/api/Yolculuk/ozet?" + sorgu);
        }).then(function (result) {
            var ozet = (result && result.data) || {};
            var cards = el("yolculuk-cards");
            clear(cards);
            cards.appendChild(card("Toplam mesafe", km(ozet.toplamKm), true));
            cards.appendChild(card("İş", km(ozet.isKm)));
            cards.appendChild(card("Özel", km(ozet.ozelKm)));
            cards.appendChild(card("İş oranı", (Number(ozet.isOrani) || 0) + " %", true));
            cards.appendChild(card("Yolculuk", String(ozet.yolculukSayisi || 0)));
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function yolculukSil(id) {
        clearMessages();
        api("/api/Yolculuk/" + id, { method: "DELETE" }).then(function (result) {
            showMessage(el("app-message"), (result && result.message) || "Kayıt silindi.", true);
            loadYolculuk();
            loadVehicles();
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function bindYolculuk() {
        el("yolculuk-form").addEventListener("submit", function (event) {
            event.preventDefault();
            clearMessages();

            if (!state.selectedVehicleId) {
                showMessage(el("app-message"), "Önce bir araç seçin.", false);
                return;
            }

            api("/api/Yolculuk", {
                method: "POST",
                body: {
                    vehicleId: state.selectedVehicleId,
                    tarih: el("yolculuk-tarih").value,
                    baslangicKm: Number(el("yolculuk-bas-km").value),
                    bitisKm: Number(el("yolculuk-bitis-km").value),
                    amac: el("yolculuk-amac").value,
                    nereden: el("yolculuk-nereden").value,
                    nereye: el("yolculuk-nereye").value,
                    not: el("yolculuk-not").value
                }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Yolculuk eklendi.", true);
                el("yolculuk-form").reset();
                el("yolculuk-tarih").value = todayInput();
                loadYolculuk();
                loadVehicles();
            }).catch(function (error) {
                handleError(el("app-message"), error);
            });
        });
    }

    function renderBelgeRows(uyeler) {
        var tbody = el("belge-rows");
        clear(tbody);

        if (uyeler.length === 0) {
            emptyRow(tbody, 4, "Ekip üyesi yok.");
            return;
        }

        uyeler.forEach(function (uye) {
            var tr = document.createElement("tr");
            tr.appendChild(make("td", uye.adSoyad));
            tr.appendChild(make("td", labelOf(TEAM_ROLES, uye.rol)));
            tr.appendChild(make("td", BELGE_DURUM[uye.enKotuDurum] || uye.enKotuDurum, "durum-" + uye.enKotuDurum.toLowerCase()));

            var belgeler = (uye.belgeler || []).map(function (belge) {
                return belge.evrakAdi + " (" + formatDate(belge.bitisTarihi) + ")";
            });
            tr.appendChild(make("td", belgeler.length === 0 ? "-" : belgeler.join(", ")));

            tbody.appendChild(tr);
        });
    }

    function loadBelgeler() {
        return api("/api/Team/belgeler").then(function (result) {
            renderBelgeRows((result && result.data) || []);
        }).catch(function () {
            clear(el("belge-rows"));
            emptyRow(el("belge-rows"), 4, "Ekip belgeleri görüntülenemedi.");
        });
    }

    function exportIndir(tur) {
        clearMessages();

        var parcalar = [];
        if (!el("export-tum-araclar").checked && state.selectedVehicleId) {
            parcalar.push("vehicleId=" + state.selectedVehicleId);
        }
        var aralik = yolculukAraligi();
        if (aralik) {
            parcalar.push(aralik);
        }

        var yol = "/api/Export/" + tur + ".csv" + (parcalar.length > 0 ? "?" + parcalar.join("&") : "");

        fetch(yol, { headers: { Authorization: "Bearer " + state.token } }).then(function (response) {
            if (!response.ok) {
                throw new Error("Dosya indirilemedi.");
            }
            return response.blob();
        }).then(function (blob) {
            var url = URL.createObjectURL(blob);
            var link = document.createElement("a");
            link.href = url;
            link.download = "garajim-" + tur + ".csv";
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            URL.revokeObjectURL(url);
            showMessage(el("app-message"), "CSV indirildi.", true);
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function bindExport() {
        ["yakit", "bakim", "masraf", "evrak"].forEach(function (tur) {
            el("export-" + tur).addEventListener("click", function () {
                exportIndir(tur);
            });
        });
    }

    var LASTIK_MEVSIM = [
        ["Yaz", "Yaz"],
        ["Kis", "Kış"],
        ["DortMevsim", "Dört mevsim"]
    ];

    var SARJ_TURU = [
        ["", "-"],
        ["Ev", "Ev"],
        ["Isyeri", "İş yeri"],
        ["HizliSarj", "Hızlı şarj"]
    ];

    function seciliArac() {
        for (var i = 0; i < state.vehicles.length; i++) {
            if (state.vehicles[i].id === state.selectedVehicleId) {
                return state.vehicles[i];
            }
        }
        return null;
    }

    function yakitAlanlariniAyarla() {
        var arac = seciliArac();
        var tur = arac ? arac.fuelType : null;
        var elektrikli = tur === "Elektrik";
        var hibrit = tur === "Hibrit";

        el("fuel-kwh-box").classList.toggle("hidden", !(elektrikli || hibrit));
        el("fuel-sarj-box").classList.toggle("hidden", !(elektrikli || hibrit));

        var litre = el("fuel-liters");
        litre.required = !elektrikli;
        litre.disabled = elektrikli;
        if (elektrikli) {
            litre.value = "";
        }

        el("fuel-kwh").required = elektrikli;
    }

    function renderLastikRows(setler) {
        var tbody = el("lastik-rows");
        clear(tbody);

        if (setler.length === 0) {
            emptyRow(tbody, 8, "Bu araç için lastik seti kaydı yok.");
            return;
        }

        setler.forEach(function (set) {
            var tr = document.createElement("tr");
            tr.appendChild(make("td", set.ad + (set.takili ? " (takılı)" : "")));
            tr.appendChild(make("td", labelOf(LASTIK_MEVSIM, set.mevsim)));
            tr.appendChild(make("td", [set.marka, set.ebat].filter(Boolean).join(" / ") || "-"));
            tr.appendChild(make("td", formatDate(set.takilmaTarihi) + " · " + km(set.takilmaKm)));
            tr.appendChild(make("td", set.sokulmeTarihi ? formatDate(set.sokulmeTarihi) + " · " + km(set.sokulmeKm) : "-"));
            tr.appendChild(make("td", set.takili ? "-" : km(set.toplamKm)));
            tr.appendChild(make("td", set.disDerinligiMm === null ? "-" : set.disDerinligiMm + " mm"));

            var islem = document.createElement("td");
            if (set.takili) {
                var sok = make("button", "Sök", "link-btn");
                sok.type = "button";
                sok.addEventListener("click", function () { lastikSok(set); });
                islem.appendChild(sok);
            }
            var sil = make("button", "Sil", "link-btn");
            sil.type = "button";
            sil.addEventListener("click", function () { lastikSil(set.id); });
            islem.appendChild(sil);
            tr.appendChild(islem);

            tbody.appendChild(tr);
        });
    }

    function loadLastik() {
        if (!state.selectedVehicleId) {
            clear(el("lastik-rows"));
            el("lastik-uyari").textContent = "";
            return Promise.resolve();
        }

        return api("/api/Lastik?vehicleId=" + state.selectedVehicleId).then(function (result) {
            var durum = (result && result.data) || {};
            el("lastik-uyari").textContent = durum.uyari || (durum.kisLastigiDonemi ? "Kış lastiği dönemindesiniz." : "");
            renderLastikRows(durum.setler || []);
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function lastikSok(set) {
        var arac = seciliArac();
        var varsayilanKm = arac ? arac.currentKm : set.takilmaKm;
        var girilen = window.prompt("Sökülme kilometresi", String(varsayilanKm));
        if (girilen === null) {
            return;
        }

        clearMessages();
        api("/api/Lastik/" + set.id + "/sok", {
            method: "PUT",
            body: {
                sokulmeTarihi: todayInput(),
                sokulmeKm: Number(girilen)
            }
        }).then(function (result) {
            showMessage(el("app-message"), (result && result.message) || "Set söküldü.", true);
            loadLastik();
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function lastikSil(id) {
        clearMessages();
        api("/api/Lastik/" + id, { method: "DELETE" }).then(function (result) {
            showMessage(el("app-message"), (result && result.message) || "Set silindi.", true);
            loadLastik();
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function bindLastik() {
        el("lastik-form").addEventListener("submit", function (event) {
            event.preventDefault();
            clearMessages();

            if (!state.selectedVehicleId) {
                showMessage(el("app-message"), "Önce bir araç seçin.", false);
                return;
            }

            var dis = el("lastik-dis").value;

            api("/api/Lastik", {
                method: "POST",
                body: {
                    vehicleId: state.selectedVehicleId,
                    ad: el("lastik-ad").value,
                    mevsim: el("lastik-mevsim").value,
                    marka: el("lastik-marka").value,
                    ebat: el("lastik-ebat").value,
                    disDerinligiMm: dis === "" ? null : Number(dis),
                    takilmaTarihi: el("lastik-tarih").value,
                    takilmaKm: Number(el("lastik-km").value)
                }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Set takıldı.", true);
                el("lastik-form").reset();
                el("lastik-tarih").value = todayInput();
                loadLastik();
            }).catch(function (error) {
                handleError(el("app-message"), error);
            });
        });
    }

    function loadDavet() {
        return api("/api/Davet").then(function (result) {
            var durum = (result && result.data) || {};
            el("davet-kod").textContent = durum.paylasimBaglantisi || durum.kod || "";
            el("davet-ozet").textContent = durum.davetSayisi + " davet · " + durum.kazanilanAracHakki + "/" + durum.ekAracUstSiniri
                + " kazanılan araç hakkı · toplam limit " + durum.aracLimiti + " araç"
                + (durum.davetEden ? " · sizi " + durum.davetEden + " davet etti" : "");

            var tbody = el("davet-rows");
            clear(tbody);
            var davetliler = durum.davetliler || [];
            if (davetliler.length === 0) {
                emptyRow(tbody, 2, "Henüz davet edilen şirket yok.");
                return;
            }
            davetliler.forEach(function (satir) {
                var tr = document.createElement("tr");
                tr.appendChild(make("td", satir.sirketAdi));
                tr.appendChild(make("td", formatDate(satir.katilmaTarihi)));
                tbody.appendChild(tr);
            });
        }).catch(function () {
            el("davet-ozet").textContent = "Davet bilgisi görüntülenemedi.";
        });
    }

    function bindDavet() {
        el("davet-kopyala").addEventListener("click", function () {
            var metin = el("davet-kod").textContent;
            if (metin && navigator.clipboard && navigator.clipboard.writeText) {
                navigator.clipboard.writeText(metin).then(function () {
                    showMessage(el("app-message"), "Davet bağlantısı kopyalandı.", true);
                });
            }
        });
    }

    function davetKodunuUrldenOku() {
        var eslesme = /[?&]davet=([A-Za-z0-9]{1,12})/.exec(window.location.search);
        if (eslesme) {
            el("register-davet").value = eslesme[1].toUpperCase();
            switchAuthTab(false);
        }
    }

    var PLAN_TURLERI = [
        ["Filo", "Filo"],
        ["Bireysel", "Bireysel"]
    ];

    function loadPanelUyarisi() {
        var kutu = el("panel-uyari");
        return api("/api/Reports/dashboard").then(function (result) {
            var panel = (result && result.data) || {};
            var plakalar = panel.kisLastigiUyariPlakalari || [];

            if (!panel.kisLastigiUyarisi || plakalar.length === 0) {
                kutu.textContent = "";
                kutu.classList.add("hidden");
                return;
            }

            kutu.textContent = panel.kisLastigiUyarisi + " Eksik araçlar: " + plakalar.join(", ");
            kutu.classList.remove("hidden");
        }).catch(function () {
            kutu.textContent = "";
            kutu.classList.add("hidden");
        });
    }

    function bindPlan() {
        el("plan-form").addEventListener("submit", function (event) {
            event.preventDefault();
            clearMessages();

            api("/api/Plan/yukseltme-talebi", {
                method: "POST",
                body: {
                    istenenPlan: el("plan-istenen").value,
                    mesaj: el("plan-mesaj").value
                }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Talebiniz iletildi.", true);
                el("plan-mesaj").value = "";
            }).catch(function (error) {
                handleError(el("app-message"), error);
            });
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
                    companyName: el("register-company").value,
                    davetKodu: el("register-davet").value
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

    var EVRAK_TYPES = [
        ["Muayene", "Muayene"],
        ["TrafikSigortasi", "Trafik sigortası"],
        ["Kasko", "Kasko"],
        ["EgzozEmisyon", "Egzoz emisyon"],
        ["KisLastigi", "Kış lastiği"],
        ["Ehliyet", "Ehliyet"],
        ["SRC", "SRC belgesi"],
        ["Psikoteknik", "Psikoteknik"]
    ];

    var EVRAK_STATUS = {
        Iyi: "İyi",
        Yaklasiyor: "Yaklaşıyor",
        Gecti: "Geçti"
    };

    function renderEvrakRows(rows) {
        var tbody = el("evrak-rows");
        clear(tbody);

        if (rows.length === 0) {
            emptyRow(tbody, 7, "Evrak kaydı yok.");
            return;
        }

        rows.forEach(function (item) {
            var tr = document.createElement("tr");
            tr.appendChild(make("td", item.evrakAdi));
            tr.appendChild(make("td", item.plaka || item.kullaniciAdi || "-"));
            tr.appendChild(make("td", formatDate(item.bitisTarihi)));
            tr.appendChild(make("td", item.kalanGun + " gün"));
            tr.appendChild(make("td", item.saglayici || "-"));
            tr.appendChild(make("td", EVRAK_STATUS[item.durum] || item.durum, "durum-" + item.durum.toLowerCase()));

            var hucre = document.createElement("td");
            if (item.aktif && canManage()) {
                var yenile = make("button", "Yenile", "link-btn");
                yenile.type = "button";
                yenile.addEventListener("click", function () { evrakYenile(item.id); });
                hucre.appendChild(yenile);
            }
            tr.appendChild(hucre);

            tbody.appendChild(tr);
        });
    }

    function loadEvrak() {
        var yol = state.selectedVehicleId ? "/api/Evrak?vehicleId=" + state.selectedVehicleId : "/api/Evrak";
        api(yol).then(function (result) {
            renderEvrakRows((result && result.data) || []);
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function loadEvrakAy() {
        var ay = el("evrak-ay").value;
        if (!ay) {
            showMessage(el("app-message"), "Önce bir ay seçin.");
            return;
        }
        clearMessages();
        api("/api/Evrak/takvim?ay=" + ay).then(function (result) {
            renderEvrakRows((result && result.data) || []);
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function evrakYenile(id) {
        clearMessages();
        api("/api/Evrak/" + id + "/yenile", { method: "POST" }).then(function (result) {
            showMessage(el("app-message"), (result && result.message) || "Evrak yenilendi.", true);
            loadEvrak();
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function bindEvrak() {
        el("evrak-form").addEventListener("submit", function (event) {
            event.preventDefault();
            clearMessages();

            if (!state.selectedVehicleId) {
                showMessage(el("app-message"), "Önce bir araç seçin.");
                return;
            }

            var bitis = el("evrak-bitis").value;
            var baslangic = el("evrak-baslangic").value;

            api("/api/Evrak", {
                method: "POST",
                body: {
                    vehicleId: state.selectedVehicleId,
                    evrakTuru: el("evrak-tur").value,
                    baslangicTarihi: baslangic ? baslangic : null,
                    bitisTarihi: bitis ? bitis : null,
                    saglayici: el("evrak-saglayici").value,
                    policeNo: el("evrak-police").value,
                    not: el("evrak-not").value
                }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Evrak eklendi.", true);
                el("evrak-form").reset();
                loadEvrak();
            }).catch(function (error) {
                handleError(el("app-message"), error);
            });
        });

        el("evrak-ay-btn").addEventListener("click", loadEvrakAy);
        el("evrak-hepsi-btn").addEventListener("click", function () {
            clearMessages();
            loadEvrak();
        });
    }

    var IMPORT_ALANLARI = [
        { key: "tarih", label: "Tarih" },
        { key: "km", label: "Kilometre" },
        { key: "litre", label: "Litre" },
        { key: "tutar", label: "Tutar" },
        { key: "birimfiyat", label: "Birim fiyat" },
        { key: "kategori", label: "Kategori" },
        { key: "aciklama", label: "Açıklama" },
        { key: "servis", label: "Servis" }
    ];

    var importDurum = { onizleme: null, hatalar: [] };

    function importAlanlari(kayitTuru) {
        if (kayitTuru === "Yakit") {
            return ["tarih", "km", "litre", "tutar", "birimfiyat"];
        }
        if (kayitTuru === "Bakim") {
            return ["tarih", "km", "tutar", "servis", "aciklama"];
        }
        return ["tarih", "tutar", "kategori", "aciklama"];
    }

    function importZorunlular(kayitTuru) {
        return kayitTuru === "Yakit" ? ["tarih", "tutar", "litre"] : ["tarih", "tutar"];
    }

    function importAlanAdi(key) {
        for (var i = 0; i < IMPORT_ALANLARI.length; i++) {
            if (IMPORT_ALANLARI[i].key === key) {
                return IMPORT_ALANLARI[i].label;
            }
        }
        return key;
    }

    function fillImportVehicles() {
        var select = el("import-arac");
        var onceki = select.value;
        clear(select);
        state.vehicles.forEach(function (vehicle) {
            var option = document.createElement("option");
            option.value = String(vehicle.id);
            option.textContent = vehicle.plate + " - " + vehicle.brand + " " + vehicle.model;
            select.appendChild(option);
        });
        if (onceki) {
            select.value = onceki;
        } else if (state.selectedVehicleId) {
            select.value = String(state.selectedVehicleId);
        }
    }

    function renderImportEslesme(onizleme) {
        var kap = el("import-eslesme");
        clear(kap);

        var zorunlular = importZorunlular(onizleme.kayitTuru);

        importAlanlari(onizleme.kayitTuru).forEach(function (alan) {
            var hucre = document.createElement("div");

            var etiket = document.createElement("label");
            etiket.setAttribute("for", "import-alan-" + alan);
            etiket.textContent = importAlanAdi(alan) + (zorunlular.indexOf(alan) >= 0 ? " *" : "");
            hucre.appendChild(etiket);

            var select = document.createElement("select");
            select.id = "import-alan-" + alan;
            select.setAttribute("data-alan", alan);

            var bos = document.createElement("option");
            bos.value = "";
            bos.textContent = "— eşlenmedi —";
            select.appendChild(bos);

            onizleme.basliklar.forEach(function (baslik, sira) {
                var option = document.createElement("option");
                option.value = String(sira);
                option.textContent = baslik || ("Sütun " + (sira + 1));
                select.appendChild(option);
            });

            var onerilen = onizleme.onerilenEslesme && onizleme.onerilenEslesme[alan];
            select.value = (onerilen === 0 || onerilen) ? String(onerilen) : "";

            hucre.appendChild(select);
            kap.appendChild(hucre);
        });
    }

    function renderImportOrnek(onizleme) {
        var baslikSatiri = el("import-ornek-baslik");
        clear(baslikSatiri);
        onizleme.basliklar.forEach(function (baslik, sira) {
            var th = document.createElement("th");
            th.textContent = baslik || ("Sütun " + (sira + 1));
            baslikSatiri.appendChild(th);
        });

        var govde = el("import-ornek-govde");
        clear(govde);
        (onizleme.ornekSatirlar || []).forEach(function (satir) {
            var tr = document.createElement("tr");
            onizleme.basliklar.forEach(function (baslik, sira) {
                var td = document.createElement("td");
                td.textContent = satir[sira] || "";
                tr.appendChild(td);
            });
            govde.appendChild(tr);
        });
    }

    function renderImportHatalar(hatalar) {
        importDurum.hatalar = hatalar || [];
        var govde = el("import-hata-govde");
        clear(govde);

        importDurum.hatalar.forEach(function (hata) {
            var tr = document.createElement("tr");

            var no = document.createElement("td");
            no.textContent = String(hata.satirNo);
            tr.appendChild(no);

            var sebep = document.createElement("td");
            sebep.textContent = hata.sebep;
            tr.appendChild(sebep);

            var icerik = document.createElement("td");
            icerik.textContent = hata.icerik || "";
            tr.appendChild(icerik);

            govde.appendChild(tr);
        });

        el("import-hata-indir").classList.toggle("hidden", importDurum.hatalar.length === 0);
    }

    function importEslesmeTopla() {
        var eslesme = {};
        var selectler = el("import-eslesme").querySelectorAll("select");
        for (var i = 0; i < selectler.length; i++) {
            var deger = selectler[i].value;
            if (deger !== "") {
                eslesme[selectler[i].getAttribute("data-alan")] = Number(deger);
            }
        }
        return eslesme;
    }

    function importSifirla() {
        importDurum.onizleme = null;
        importDurum.hatalar = [];
        el("import-dosya").value = "";
        el("import-onizleme").classList.add("hidden");
        el("import-sonuc").classList.add("hidden");
        clear(el("import-eslesme"));
        clear(el("import-ornek-baslik"));
        clear(el("import-ornek-govde"));
        clear(el("import-hata-govde"));
    }

    function importDosya() {
        var girdi = el("import-dosya");
        return girdi.files && girdi.files.length > 0 ? girdi.files[0] : null;
    }

    function importUygula(dryRun) {
        clearMessages();

        var dosya = importDosya();
        if (!dosya) {
            showMessage(el("app-message"), "Önce bir CSV dosyası seçin.", false);
            return;
        }

        var aracId = el("import-arac").value;
        if (!aracId) {
            showMessage(el("app-message"), "Önce bir araç seçin.", false);
            return;
        }

        var kayitTuru = el("import-tur").value;
        var eslesme = importEslesmeTopla();

        var eksik = importZorunlular(kayitTuru).filter(function (alan) {
            return !(alan in eslesme);
        });
        if (eksik.length > 0) {
            showMessage(el("app-message"), "Zorunlu sütunlar eşlenmeli: " + eksik.map(importAlanAdi).join(", "), false);
            return;
        }

        var form = new FormData();
        form.append("file", dosya);
        form.append("kayitTuru", kayitTuru);
        form.append("vehicleId", aracId);
        form.append("eslesme", JSON.stringify(eslesme));
        form.append("dryRun", dryRun ? "true" : "false");

        api("/api/Import/uygula", { method: "POST", body: form }).then(function (result) {
            var veri = result.data;
            el("import-sonuc").classList.remove("hidden");
            el("import-sonuc-ozet").textContent = (veri.dryRun ? "Deneme: " : "Sonuç: ")
                + veri.eklenen + (veri.dryRun ? " kayıt eklenecek, " : " kayıt eklendi, ")
                + veri.atlanan + " mükerrer atlandı, "
                + (veri.hatali || []).length + " satır hatalı.";
            renderImportHatalar(veri.hatali);

            showMessage(el("app-message"), (result && result.message) || "İşlem tamamlandı.", true);

            if (!veri.dryRun && veri.eklenen > 0) {
                loadVehicles();
                loadActiveTab();
            }
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function importHatalariIndir() {
        if (importDurum.hatalar.length === 0) {
            return;
        }

        var satirlar = ["satir;sebep;icerik"];
        importDurum.hatalar.forEach(function (hata) {
            satirlar.push(hata.satirNo + ";" + (hata.sebep || "").replace(/;/g, ",") + ";" + (hata.icerik || "").replace(/;/g, ","));
        });

        var blob = new Blob(["﻿" + satirlar.join("\r\n")], { type: "text/csv;charset=utf-8" });
        var url = URL.createObjectURL(blob);
        var link = document.createElement("a");
        link.href = url;
        link.download = "hatali-satirlar.csv";
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    }

    function bindImport() {
        el("import-onizle").addEventListener("click", function () {
            clearMessages();

            var dosya = importDosya();
            if (!dosya) {
                showMessage(el("app-message"), "Önce bir CSV dosyası seçin.", false);
                return;
            }

            var form = new FormData();
            form.append("file", dosya);
            form.append("kayitTuru", el("import-tur").value);

            api("/api/Import/onizle", { method: "POST", body: form }).then(function (result) {
                var veri = result.data;
                importDurum.onizleme = veri;

                el("import-onizleme").classList.remove("hidden");
                el("import-sonuc").classList.add("hidden");
                el("import-ozet").textContent = veri.sablon + " biçimi sezildi. Ayraç: " + veri.ayrac
                    + ", " + veri.toplamSatir + " satır, "
                    + (veri.hataliSatirlar || []).length + " satır okunamadı.";

                renderImportEslesme(veri);
                renderImportOrnek(veri);
                renderImportHatalar(veri.hataliSatirlar);
                fillImportVehicles();
            }).catch(function (error) {
                handleError(el("app-message"), error);
            });
        });

        el("import-tur").addEventListener("change", function () {
            if (importDurum.onizleme) {
                importDurum.onizleme.kayitTuru = el("import-tur").value;
                renderImportEslesme(importDurum.onizleme);
            }
        });

        el("import-deneme").addEventListener("click", function () {
            importUygula(true);
        });

        el("import-uygula").addEventListener("click", function () {
            importUygula(false);
        });

        el("import-sifirla").addEventListener("click", importSifirla);
        el("import-hata-indir").addEventListener("click", importHatalariIndir);
    }

    function bindAyarlar() {
        el("ayarlar-btn").addEventListener("click", function () {
            var kutu = el("ayarlar-box");
            kutu.classList.toggle("hidden", !kutu.classList.contains("hidden"));
            if (!kutu.classList.contains("hidden")) {
                fillImportVehicles();
                loadDavet();
            }
        });

        el("ayarlar-close").addEventListener("click", function () {
            el("ayarlar-box").classList.add("hidden");
        });

        el("takvim-olustur").addEventListener("click", function () {
            clearMessages();
            api("/api/Takvim/abonelik", { method: "POST" }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Abonelik oluşturuldu.", true);
                el("takvim-sonuc").classList.remove("hidden");
                el("takvim-url").textContent = result.data.url;
            }).catch(function (error) {
                handleError(el("app-message"), error);
            });
        });

        el("takvim-kapat").addEventListener("click", function () {
            clearMessages();
            api("/api/Takvim/abonelik", { method: "DELETE" }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Abonelik kapatıldı.", true);
                el("takvim-sonuc").classList.add("hidden");
            }).catch(function (error) {
                handleError(el("app-message"), error);
            });
        });

        el("takvim-kopyala").addEventListener("click", function () {
            var metin = el("takvim-url").textContent;
            if (metin && navigator.clipboard && navigator.clipboard.writeText) {
                navigator.clipboard.writeText(metin).then(function () {
                    showMessage(el("app-message"), "Bağlantı kopyalandı.", true);
                });
            }
        });
    }

    function karneKapsamiOku() {
        return {
            bakimGecmisi: el("karne-bakim").checked,
            parcaHafizasi: el("karne-parca").checked,
            yakitOzeti: el("karne-yakit").checked,
            belgeler: el("karne-belge").checked,
            plakaGoster: el("karne-plaka").checked,
            tutarGoster: el("karne-tutar").checked,
            acilKart: el("karne-acil").checked
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

    function taslagiInceleyeAc(taslakId) {
        clearMessages();
        api("/api/Receipts/" + taslakId).then(function (result) {
            el("receipt-box").classList.remove("hidden");
            showReceiptReview(result.data);
            el("receipt-review").scrollIntoView({ behavior: "smooth", block: "nearest" });
        }).catch(function (error) {
            handleError(el("app-message"), error);
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

            var actions = make("span", "", "row-actions");
            var incele = make("button", "İncele", "link-btn");
            incele.type = "button";
            incele.addEventListener("click", function () { taslagiInceleyeAc(s.veri.taslakId); });
            actions.appendChild(incele);
            li.appendChild(actions);

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
                    liters: el("fuel-liters").value === "" ? 0 : Number(el("fuel-liters").value),
                    kwh: el("fuel-kwh").value === "" ? null : Number(el("fuel-kwh").value),
                    sarjTuru: el("fuel-sarj").value === "" ? null : el("fuel-sarj").value,
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
            loadMaliyet();
            loadFiloMaliyet();
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
        fillSelect(el("evrak-tur"), EVRAK_TYPES);
        fillSelect(el("yolculuk-amac"), YOLCULUK_AMAC);
        fillSelect(el("lastik-mevsim"), LASTIK_MEVSIM);
        fillSelect(el("fuel-sarj"), SARJ_TURU);
        fillSelect(el("plan-istenen"), PLAN_TURLERI);
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
        el("yolculuk-tarih").value = today;
        el("lastik-tarih").value = today;
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
        bindEvrak();
        bindAyarlar();
        bindImport();
        bindYolculuk();
        bindExport();
        bindLastik();
        bindDavet();
        bindPlan();
        davetKodunuUrldenOku();

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

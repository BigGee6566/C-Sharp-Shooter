<%@ Page Title="Register" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="Register.aspx.cs" Inherits="ForteLogisticsApp.RegisterPage" %>

<asp:Content ID="c1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="flex items-center justify-center min-h-[70vh]">
        <div class="w-full max-w-[560px] bg-surface-container-lowest border border-outline-variant rounded-xl shadow-lg p-8 md:p-12">

            <div class="mb-stack-sm">
                <a href="<%= ResolveUrl("~/Default.aspx") %>" class="inline-flex items-center gap-1 font-label-sm text-label-sm text-on-surface-variant hover:text-primary transition-colors">
                    <span class="material-symbols-outlined">arrow_back</span> Back to Home
                </a>
            </div>

            <div class="text-center mb-stack-md">
                <div class="flex justify-center items-center gap-base mb-base">
                    <span class="material-symbols-outlined ms-32 text-primary">local_shipping</span>
                    <span class="font-headline-md text-headline-md font-bold text-primary">LogistiCore</span>
                </div>
                <h1 class="font-headline-lg text-headline-lg-mobile md:text-headline-lg text-on-surface mb-2">Join LogistiCore</h1>
                <p class="font-body-lg text-body-md text-on-surface-variant">Start shipping with precision and speed.</p>
            </div>

            <!-- Account type. Individual and Business both create a customer account; the
                 choice is stored as Client.ClientType. Staff accounts are provisioned by an
                 administrator, so that tab explains rather than submits. -->
            <asp:HiddenField ID="hfRole" runat="server" Value="Individual" />
            <div class="flex bg-surface-container-high rounded-lg p-1 mb-stack-md border border-outline-variant">
                <button type="button" id="tab-individual" onclick="switchRole('Individual'); return false;"
                        class="flex-1 py-2 px-4 rounded-lg font-label-md text-label-md text-center bg-surface-container-lowest shadow-sm text-primary">Individual</button>
                <button type="button" id="tab-business" onclick="switchRole('Business'); return false;"
                        class="flex-1 py-2 px-4 rounded-lg font-label-md text-label-md text-center text-on-surface-variant">Business</button>
                <button type="button" id="tab-staff" onclick="switchRole('Staff'); return false;"
                        class="flex-1 py-2 px-4 rounded-lg font-label-md text-label-md text-center text-on-surface-variant">Staff</button>
            </div>

            <!-- Shown for the Staff tab only. -->
            <asp:Panel ID="pnlStaffNotice" runat="server" style="display:none" CssClass="fcl-alert fcl-alert-info mb-stack-md">
                Staff accounts are created by a system administrator, not through self-registration.
                Please contact your operations manager to have an account provisioned for you.
            </asp:Panel>

            <asp:Panel ID="pnlForm" runat="server" CssClass="flex flex-col gap-4">

                <div>
                    <label class="block font-label-sm text-label-sm text-on-surface-variant mb-1" for="<%= txtFullName.ClientID %>">
                        <span id="lblNameText">Full Name</span> *</label>
                    <asp:TextBox ID="txtFullName" runat="server" CssClass="fcl-input" placeholder="Jane Doe" />
                    <asp:RequiredFieldValidator ID="rfvFullName" runat="server" ControlToValidate="txtFullName"
                        Display="Dynamic" ValidationGroup="Register" ErrorMessage="Full name is required."
                        CssClass="block font-label-sm text-label-sm text-error mt-1" />
                </div>

                <div>
                    <label class="block font-label-sm text-label-sm text-on-surface-variant mb-1" for="<%= txtEmail.ClientID %>">Email Address *</label>
                    <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" CssClass="fcl-input" placeholder="jane@example.com" />
                    <asp:RequiredFieldValidator ID="rfvEmail" runat="server" ControlToValidate="txtEmail"
                        Display="Dynamic" ValidationGroup="Register" ErrorMessage="Email address is required."
                        CssClass="block font-label-sm text-label-sm text-error mt-1" />
                    <asp:RegularExpressionValidator ID="revEmail" runat="server" ControlToValidate="txtEmail"
                        Display="Dynamic" ValidationGroup="Register" ErrorMessage="Please enter a valid email address."
                        ValidationExpression="^[^@\s]+@[^@\s]+\.[^@\s]+$"
                        CssClass="block font-label-sm text-label-sm text-error mt-1" />
                </div>

                <div>
                    <label class="block font-label-sm text-label-sm text-on-surface-variant mb-1" for="<%= txtPhone.ClientID %>">Phone Number</label>
                    <asp:TextBox ID="txtPhone" runat="server" TextMode="Phone" CssClass="fcl-input" placeholder="071 234 5678" />
                </div>

                <div>
                    <label class="block font-label-sm text-label-sm text-on-surface-variant mb-1" for="<%= txtAddress.ClientID %>">Address</label>
                    <asp:TextBox ID="txtAddress" runat="server" TextMode="MultiLine" Rows="2" CssClass="fcl-input" placeholder="12 Main Road, Isando, Gauteng" />
                </div>

                <div>
                    <label class="block font-label-sm text-label-sm text-on-surface-variant mb-1" for="<%= txtPassword.ClientID %>">Password *</label>
                    <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" CssClass="fcl-input" placeholder="At least 6 characters" />
                    <asp:RequiredFieldValidator ID="rfvPassword" runat="server" ControlToValidate="txtPassword"
                        Display="Dynamic" ValidationGroup="Register" ErrorMessage="Password is required."
                        CssClass="block font-label-sm text-label-sm text-error mt-1" />
                </div>

                <div>
                    <label class="block font-label-sm text-label-sm text-on-surface-variant mb-1" for="<%= txtConfirm.ClientID %>">Confirm Password *</label>
                    <asp:TextBox ID="txtConfirm" runat="server" TextMode="Password" CssClass="fcl-input" placeholder="Re-enter your password" />
                    <asp:CompareValidator ID="cvPassword" runat="server" ControlToValidate="txtConfirm" ControlToCompare="txtPassword"
                        Display="Dynamic" ValidationGroup="Register" ErrorMessage="The passwords you entered do not match."
                        CssClass="block font-label-sm text-label-sm text-error mt-1" />
                </div>

                <label class="flex items-start gap-2 cursor-pointer">
                    <asp:CheckBox ID="chkTerms" runat="server" CssClass="mt-1" />
                    <span class="font-body-md text-label-sm text-on-surface-variant">
                        I agree to the Terms of Service and Privacy Policy.</span>
                </label>

                <asp:Label ID="lblStatus" runat="server" Visible="false" />

                <asp:Button ID="btnRegister" runat="server" Text="Create Account" OnClick="btnRegister_Click"
                    ValidationGroup="Register" CssClass="fcl-btn w-full mt-2" />
            </asp:Panel>

            <p class="text-center font-body-md text-label-sm text-on-surface-variant mt-stack-md">
                Already have an account?
                <a class="fcl-link font-semibold" href="<%= ResolveUrl("~/Login.aspx") %>">Sign in</a>.
            </p>
        </div>
    </div>
</asp:Content>

<asp:Content ID="c2" ContentPlaceHolderID="ScriptContent" runat="server">
    <script>
        function switchRole(role) {
            document.getElementById('<%= hfRole.ClientID %>').value = role;

            var on = 'flex-1 py-2 px-4 rounded-lg font-label-md text-label-md text-center bg-surface-container-lowest shadow-sm text-primary';
            var off = 'flex-1 py-2 px-4 rounded-lg font-label-md text-label-md text-center text-on-surface-variant';
            var tabs = { 'Individual': 'tab-individual', 'Business': 'tab-business', 'Staff': 'tab-staff' };

            for (var key in tabs) {
                document.getElementById(tabs[key]).className = (key === role) ? on : off;
            }

            // Staff cannot self-register; swap the form for the explanation.
            document.getElementById('<%= pnlForm.ClientID %>').style.display = (role === 'Staff') ? 'none' : '';
            document.getElementById('<%= pnlStaffNotice.ClientID %>').style.display = (role === 'Staff') ? '' : 'none';

            var nameLabel = document.getElementById('lblNameText');
            if (nameLabel) { nameLabel.textContent = (role === 'Business') ? 'Company Name' : 'Full Name'; }
        }
    </script>
</asp:Content>

import * as React from 'react';
import { observer } from 'mobx-react';

import {
    Box, Button, Dialog, DialogActions, DialogContent, DialogContentText, DialogTitle, TextField
} from '@material-ui/core';

import { OrchestrationDetailsState } from '../states/OrchestrationDetailsState';

// Buttons for detailed orchestration view
@observer
export class OrchestrationButtons extends React.Component<{ state: OrchestrationDetailsState }> {

    render(): JSX.Element {
        const state = this.props.state;

        return (<>

            {this.renderDialogs(state)}

            <Button variant="outlined" color="primary" size="large" onClick={() => state.rewindConfirmationOpen = true}>
                Rewind
                </Button>
            <Box width={20} />
            <Button variant="outlined" color="primary" size="large" onClick={() => state.terminateConfirmationOpen = true}>
                Terminate
            </Button>
            <Box width={20} />
            <Button variant="outlined" color="primary" size="large" onClick={() => state.dialogOpen = true}>
                Raise Event
            </Button>
            <Box width={20} />
            <Button variant="outlined" color="primary" size="large" onClick={() => state.purgeConfirmationOpen = true}>
                Purge
            </Button>            
            
        </>);
    }

    private renderDialogs(state: OrchestrationDetailsState): JSX.Element {
        return (<>

            <Dialog
                open={state.rewindConfirmationOpen}
                onClose={() => state.rewindConfirmationOpen = false}
            >
                <DialogTitle>Confirm Rewind</DialogTitle>
                <DialogContent>
                    <DialogContentText>
                        You're about to rewind orchestration '{state.orchestrationId}'. Are you sure?
                    </DialogContentText>
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => state.rewindConfirmationOpen = false} color="primary" autoFocus>
                        Cancel
                    </Button>
                    <Button onClick={() => state.rewind()} color="secondary">
                        Yes, rewind
                    </Button>
                </DialogActions>
            </Dialog>

            <Dialog
                open={state.terminateConfirmationOpen}
                onClose={() => state.terminateConfirmationOpen = false}
            >
                <DialogTitle>Confirm Terminate</DialogTitle>
                <DialogContent>
                    <DialogContentText>
                        You're about to terminate orchestration '{state.orchestrationId}'. This operation cannot be undone. Are you sure?
                    </DialogContentText>
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => state.terminateConfirmationOpen = false} color="primary" autoFocus>
                        Cancel
                    </Button>
                    <Button onClick={() => state.terminate()} color="secondary">
                        Yes, terminate
                    </Button>
                </DialogActions>
            </Dialog>

            <Dialog
                open={state.dialogOpen}
                onClose={() => state.dialogOpen = false}
            >
                <DialogTitle>Raise Event</DialogTitle>
                <DialogContent>
                    <DialogContentText>
                        Provide event name and some additional data.
                    </DialogContentText>

                    <TextField
                        autoFocus
                        margin="dense"
                        label="Event Name"
                        fullWidth
                        value={state.eventName}
                        onChange={(evt) => state.eventName = evt.target.value as string}
                    />

                    <TextField
                        margin="dense"
                        label="Event Data (JSON)"
                        fullWidth
                        multiline
                        rows={7}
                        value={state.eventData}
                        onChange={(evt) => state.eventData = evt.target.value as string}
                    />

                </DialogContent>
                <DialogActions>
                    <Button onClick={() => state.dialogOpen = false} color="primary">
                        Cancel
                    </Button>
                    <Button onClick={() => state.raiseEvent()} disabled={!state.eventName} color="secondary">
                        Raise
                    </Button>
                </DialogActions>
            </Dialog>

            <Dialog
                open={state.purgeConfirmationOpen}
                onClose={() => state.purgeConfirmationOpen = false}
            >
                <DialogTitle>Confirm Purge</DialogTitle>
                <DialogContent>
                    <DialogContentText>
                        You're about to purge orchestration '{state.orchestrationId}'. This operation drops orchestration state from the underlying storage and cannot be undone. Are you sure?
                    </DialogContentText>
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => state.purgeConfirmationOpen = false} color="primary" autoFocus>
                        Cancel
                    </Button>
                    <Button onClick={() => state.purge()} color="secondary">
                        Yes, purge
                    </Button>
                </DialogActions>
            </Dialog>
        </>);
    }
}